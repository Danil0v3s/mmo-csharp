using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Combat;

/// <summary>
/// Renewal port of rAthena's <c>battle_calc_weapon_attack</c>
/// (battle.cpp:7635). Trimmed first slice — covers the standard melee
/// path used by mob/player auto-attack. Skill ratios, dual-wield,
/// arrow path, card fixes, masteries, status modifiers all plug into
/// the same skeleton as their owning subsystems land.
///
/// Order of operations mirrors rAthena exactly:
///   1. Critical roll           (is_attack_critical)
///   2. Hit roll                (is_attack_hitting → flee, perfect dodge)
///   3. Base damage             (battle_calc_base_damage, renewal)
///   4. Element fix             (battle_attr_fix)
///   5. Size fix                (already in step 3 for PCs via atkmods)
///   6. Defense reduction       (battle_calc_defense_reduction, renewal)
///   7. Min damage of 1         (battle_min_damage)
/// </summary>
public sealed class BattleCalculator : IBattleCalculator
{
    // COMBAT-77 — battle_config.max_res_mres_ignored (config/battle/player.json: 50). The
    // cap on the attacker's combined Res/MRes-ignore % (battle.cpp:7838).
    private const int MaxResMresIgnored = 50;

    private readonly Random _rng;
    private readonly IBattleCardService? _cards;
    // COMBAT-59 — explicit SC (tests inject `sc:`) OR a lazy seam that breaks the
    // production cycle IStatusChangeService → IDamageService → IBattleCalculator.
    // The `_sc` accessor prefers the explicit value, then resolves the lazy one on
    // first combat read (well after startup), so live SC-gated combat reads activate.
    private readonly IStatusChangeService? _scExplicit;
    private readonly Lazy<IStatusChangeService>? _scLazy;
    private IStatusChangeService? _sc => _scExplicit ?? _scLazy?.Value;
    private readonly IMadoGearService? _mado;
    // COMBAT-19 — per-skill element resolver (battle_get_magic/misc_element).
    // Optional: when null, magic/misc fall back to the caster's weapon element
    // (the legacy behavior), so existing call sites/tests are unaffected.
    private readonly IBattleElementService? _elements;
    // COMBAT-20 — GvG/BG zone damage scaling (battle_calc_attack_gvg_bg).
    // Optional: null on a non-zone-aware build → no scaling.
    private readonly IZoneDamageService? _zone;
    // COMBAT-37 — live equipped-ammo count for the Fear Breeze div cap.
    // Optional: null → Fear Breeze (which requires ammo > 1) can't trigger.
    private readonly Map.Server.Inventory.IAmmoService? _ammo;

    public BattleCalculator(Random? rng = null, IBattleCardService? cards = null, IStatusChangeService? sc = null,
        IMadoGearService? mado = null, IBattleElementService? elements = null, IZoneDamageService? zone = null,
        Map.Server.Inventory.IAmmoService? ammo = null, Lazy<IStatusChangeService>? scLazy = null)
    {
        _rng = rng ?? Random.Shared;
        _cards = cards;
        _scExplicit = sc;
        _scLazy = scLazy;
        _mado = mado;
        _elements = elements;
        _zone = zone;
        _ammo = ammo;
    }

    public BattleDamage CalcWeaponAttack(Entity source, Entity target)
        => CalcWeaponAttack(source, target, skillId: 0);

    public BattleDamage CalcWeaponAttack(Entity source, Entity target, ushort skillId)
    {
        var result = new BattleDamage { Lane = BattleAttackType.Weapon };
        var s = source.Stats;
        var t = target.Stats;
        var srcIsPc = source is PlayerEntity;
        var tgtIsPc = target is PlayerEntity;

        // --- Step 1: critical roll  (battle.cpp:2948 is_attack_critical) ---
        bool isCritical = TryCritical(source, target, s, t, srcIsPc, tgtIsPc);

        // --- Step 2: perfect dodge / flee  (battle.cpp:3154 is_attack_hitting) ---
        if (!isCritical)
        {
            // Lucky dodge — tstatus->flee2 is stored at 10× display
            // (rAthena status.cpp:2689). Roll in [0,1000); pass = miss.
            // T5.3c: emit LuckyDodge (rAthena DMG_LUCY_DODGE) so the
            // client renders the "Lucky!" overlay instead of a plain
            // dodge animation.
            if (t.Flee2 > 0 && _rng.Next(1000) < t.Flee2)
            {
                result.Type = Core.Server.Packets.Out.ZC.DamageActionType.LuckyDodge;
                return result;
            }

            // Renewal default hitrate = 0 + sstatus->hit - tstatus->flee.
            int hitrate = 0 + s.Hit - t.Flee;
            hitrate = Math.Clamp(hitrate, 5, 100); // battle.cpp:3372 — clamp 5..100

            if (_rng.Next(100) >= hitrate)
            {
                result.Type = Core.Server.Packets.Out.ZC.DamageActionType.Flee;
                return result;
            }
        }

        // --- Step 3: base damage  (battle.cpp:2453 battle_calc_base_damage, renewal) ---
        // P0.3 — SC_MAXIMIZEPOWER forces the weapon roll to its max value
        // (Whitesmith Maximize Power; rAthena status.cpp:11268). Pass the
        // SC presence through so CalcBaseDamage picks atkMax.
        var forceMaxRoll = _sc?.Get(source, StatusType.Maximizepower) != null;

        // COMBAT-18 — right-hand damage. Steps 3-7 (base → atkRate → sizeMod →
        // element → def → mastery → cardfix → caster SC bumps → floor) run
        // through the shared ComputeHandDamage so the left hand gets the
        // identical pipeline.
        int rightWeaponType = srcIsPc ? ((source as PlayerEntity)?.WeaponType ?? 0) : 0;
        long damage = ComputeHandDamage(
            source, target, s, t, srcIsPc, isCritical, forceMaxRoll,
            atkMin: s.WatkMin, atkMax: s.WatkMax, weaponLevel: s.WeaponLevel,
            weaponType: rightWeaponType, weaponElement: s.WeaponElement, leftHand: false);

        // COMBAT-18 — left-hand (dual-wield) damage. Only a PC on a normal
        // attack with an off-hand weapon (lhw.atk > 0) is left-handed
        // (is_attack_left_handed, battle.cpp:2917). Mobs never dual-wield.
        long damage2 = 0;
        if (srcIsPc && s.LeftWatkMax > 0)
        {
            damage2 = ComputeHandDamage(
                source, target, s, t, srcIsPc, isCritical, forceMaxRoll,
                atkMin: s.LeftWatkMin, atkMax: s.LeftWatkMax, weaponLevel: s.LeftWeaponLevel,
                weaponType: s.LeftWeaponType, weaponElement: s.LeftWeaponElement, leftHand: true);
        }

        // COMBAT-61/78 — bonus bCritAtkRate (battle.cpp:7787). On a critical the
        // per-hand damage gains +crit_atk_rate %. The auto-attack branch (skill_id == 0)
        // uses ÷100 and is applied here. For a skill swing (skillId > 0) the bump is
        // SUPPRESSED here — ComputeSkillDamage applies it with the ÷200 skill divisor
        // AFTER the skill ratio, so it isn't double-counted. PC sources only; commutes
        // through the left/right split.
        if (skillId == 0 && isCritical && srcIsPc
            && (source as PlayerEntity)?.EquipBonuses is { CritAtkRate: var car } && car != 0)
        {
            damage += damage * car / 100;
            if (damage2 > 0) damage2 += damage2 * car / 100;
        }

        // COMBAT-18 — battle_calc_attack_left_right_hands (battle.cpp:7150):
        // katar off-hand fraction + the thief / kagerou right/left masteries.
        ApplyLeftRightSplit(source, rightWeaponType, ref damage, ref damage2);

        result.Damage = damage;
        result.Damage2 = damage2;
        result.Type = isCritical
            ? Core.Server.Packets.Out.ZC.DamageActionType.Critical
            : Core.Server.Packets.Out.ZC.DamageActionType.Normal;

        // COMBAT-17 — auto-attack multi-hit (battle.cpp:4394
        // battle_calc_multi_attack). Only the no-skill_id (auto-attack)
        // branch runs here; per-skill div_ overrides live on the skill
        // plugins. On a successful double-attack roll the single swing
        // becomes a positive-div multi-hit: div_ = skill_get_num(TF_DOUBLE)
        // = 2 and DAMAGE_DIV_FIX multiplies the per-hit damage by the count
        // (battle.cpp:4365). DAMAGE_DIV_FIX only multiplies wd.damage (the
        // right hand), so Damage2 is untouched here.
        CalcMultiAttack(source, target, result);

        // Wave 66 / Track B — populate Damage.dmotion + Damage.walkdelay.
        // rAthena: Damage struct's dmotion is the target's Amotion (capped
        // at 2000ms), and walkdelay is half of that with a 80ms floor on
        // damaging hits. Misses (handled in early-return paths above)
        // produce Damage=0 with DMotion/WalkDelay=0. Reference: battle.cpp
        // battle_drain's neighbors that set wd.dmotion + wd.amotion right
        // after the hit-roll resolution.
        PopulateMotionFields(result, t);

        // Wave 87c — Mado Gear overheat accumulator (battle.cpp:2031).
        // Each weapon swing while in Mado mode bumps SC_OVERHEAT_LIMITPOINT.
        // Fire-element weapons add 3 heat per hit, all others add 1.
        // SC_OVERHEAT starts when the limitpoint hits the cap, draining
        // HP per tick until a cooling device is used (NC_EMERGENCYCOOL).
        if (_mado != null && _sc != null && source is PlayerEntity pcMado
            && _sc.Get(pcMado, StatusType.Madogear) != null)
        {
            var rightEle = s.WeaponElement == 0 ? BattleElement.Neutral : (BattleElement)s.WeaponElement;
            int heat = rightEle == BattleElement.Fire ? 3 : 1;
            _mado.AddHeat(pcMado, heat);
        }
        return result;
    }

    /// <summary>
    /// rAthena <c>Damage.dmotion</c> + <c>Damage.walkdelay</c> derivation.
    /// dmotion = clamp(target.Amotion - 50, 0, 2000) — the rAthena code
    /// stores target's animation motion minus a 50ms server-side trim so
    /// the next swing tick lines up with the client's hit-stun
    /// animation. walkdelay = max(80, dmotion/2) when the hit lands, 0
    /// on miss. Sets both on <paramref name="result"/>.
    /// </summary>
    private static void PopulateMotionFields(BattleDamage result, BattleStats target)
    {
        if (!result.DidHit || result.Total <= 0)
        {
            result.DMotion = 0;
            result.WalkDelay = 0;
            return;
        }
        int dmotion = Math.Clamp(target.Amotion - 50, 0, 2000);
        result.DMotion = dmotion;
        result.WalkDelay = Math.Max(80, dmotion / 2);
    }

    /// <summary>
    /// COMBAT-17 — auto-attack slice of <c>battle_calc_multi_attack</c>
    /// (battle.cpp:4394). rAthena only enters this for a PC source with no
    /// skill_id (a plain auto-attack swing); the per-skill div_ switch arms
    /// (RK_WINDCUTTER, SC_FATALMENACE, SR_RIDEINLIGHTNING …) and the
    /// SC_FEARBREEZE bow multi-shot / GS_CHAINACTION revolver chain are
    /// separate triggers tracked in <c>COMBAT-37</c>.
    ///
    /// Double-attack triggers (battle.cpp:4438-4458), highest single rate wins:
    ///   * TF_DOUBLE learned AND dagger equipped,
    ///   * <c>bonus.double_rate &gt; 0</c> AND not bare-handed,
    ///   * SC_KAGEMUSYA active AND not bare-handed.
    /// Renewal success rate = <c>max(7 * TF_DOUBLE_lv, double_rate)</c>, or
    /// <c>SC_KAGEMUSYA.Val1 * 10</c> when the shadow clone is up. On success
    /// div_ becomes 2 (<c>skill_get_num(TF_DOUBLE)</c>) and the per-hit damage
    /// is doubled (positive-div <c>DAMAGE_DIV_FIX</c>). A critical double-swing
    /// keeps the critical animation but still renders 2 hits.
    /// </summary>
    private void CalcMultiAttack(Entity source, Entity target, BattleDamage result)
    {
        // sd && !skill_id — auto-attack, PC only. Mob double-attack is
        // handled by mob skill data, not this branch.
        if (source is not PlayerEntity sd) return;
        // Already multi (another branch / skill) — don't stack.
        if (result.Hits != 1) return;
        // A miss / perfect dodge never multi-hits.
        if (!result.DidHit || result.Total <= 0) return;

        int weapon = sd.WeaponType;

        // rAthena battle_calc_multi_attack runs the branches in order, each
        // gated on div_ == 1 (no stacking): Fear Breeze → double-attack →
        // Chain Action (battle.cpp:4400-4467).
        if (result.Hits == 1) TryFearBreeze(sd, weapon, result);
        if (result.Hits == 1) TryDoubleAttack(sd, weapon, result);
        if (result.Hits == 1) TryChainAction(sd, target, weapon, result);
    }

    /// <summary>
    /// SC_FEARBREEZE auto-attack multi-hit (battle.cpp:4403): bow + equipped ammo
    /// &gt; 1. A single <c>rnd()%100</c> roll picks the hit count via the val1
    /// tier ladder (val5 ≤4%→5, ≤7%→4, ≤10%→3, ≤13%→2; lower val1 starts further
    /// down the ladder), then caps div by the ammo amount.
    /// </summary>
    private void TryFearBreeze(PlayerEntity sd, int weapon, BattleDamage result)
    {
        if (weapon != Map.Server.Inventory.WeaponTypeCodes.Bow) return;
        var fb = _sc?.Get(sd, StatusType.Fearbreeze);
        if (fb == null) return;
        int ammo = _ammo?.GetEquippedAmmoAmount(sd) ?? 0;
        if (ammo <= 1) return;

        int chance = _rng.Next(100);
        int div = 1;
        // Tier ladder with fall-through (rAthena's switch fallthrough): a higher
        // val1 first tries its own tier, then the lower tiers' wider windows.
        if (fb.Val1 >= 5 && chance < 4) div = 5;
        else if (fb.Val1 >= 4 && chance < 7) div = 4;
        else if (fb.Val1 >= 3 && chance < 10) div = 3;
        else if (fb.Val1 >= 1 && chance < 13) div = 2;
        if (div <= 1) return;

        div = Math.Min(div, ammo);          // div_ = min(div_, ammo amount)
        fb.Val4 = div - 1;                  // rAthena stores val4 = div_-1
        if (div <= 1) return;
        ApplyMultiHit(result, div);
    }

    /// <summary>
    /// TF_DOUBLE / <c>bonus.double_rate</c> / SC_KAGEMUSYA double-attack
    /// (battle.cpp:4437, COMBAT-17). skill_get_num(TF_DOUBLE) = 2.
    /// </summary>
    private void TryDoubleAttack(PlayerEntity sd, int weapon, BattleDamage result)
    {
        bool isFist = Map.Server.Inventory.WeaponTypeCodes.IsFist(weapon);
        int tfDoubleLv = sd.LearnedSkills.TryGetValue(Map.Server.Skills.SkillIds.TF_DOUBLE, out var lv) ? lv : 0;
        var kagemusya = _sc?.Get(sd, StatusType.Kagemusya);
        int doubleRate = sd.EquipBonuses?.DoubleRate ?? 0;

        bool eligible =
            (tfDoubleLv > 0 && Map.Server.Inventory.WeaponTypeCodes.IsDagger(weapon))
            || (doubleRate > 0 && !isFist)
            || (kagemusya != null && !isFist);
        if (!eligible) return;

        // Success chance is not additive — the higher one is used (Skotlex).
        int maxRate = kagemusya != null
            ? kagemusya.Val1 * 10                 // same rate as even TF_DOUBLE levels
            : Math.Max(7 * tfDoubleLv, doubleRate); // RENEWAL

        if (_rng.Next(100) >= maxRate) return;
        ApplyMultiHit(result, 2);
    }

    /// <summary>
    /// Gunslinger Chain Action (battle.cpp:4459): revolver + GS_CHAINACTION, or
    /// SC_E_CHAIN (Eternal Chain) val1. <c>rnd()%100 &lt; 5*lv</c> →
    /// div_ = skill_get_num(GS_CHAINACTION) = 2, and starts SC_QD_SHOT_READY for
    /// skill_get_time(RL_QD_SHOT,1) = 1500 ms (val1 = target id).
    /// </summary>
    private void TryChainAction(PlayerEntity sd, Entity target, int weapon, BattleDamage result)
    {
        int skillLv = 0;
        if (weapon == Map.Server.Inventory.WeaponTypeCodes.Revolver
            && sd.LearnedSkills.TryGetValue(Map.Server.Skills.SkillIds.GS_CHAINACTION, out var ca) && ca > 0)
            skillLv = ca;
        else if (_sc?.Get(sd, StatusType.EChain) is { Val1: > 0 } echain)
            skillLv = echain.Val1;
        if (skillLv <= 0) return;

        if (_rng.Next(100) >= 5 * skillLv) return;

        ApplyMultiHit(result, 2);            // skill_get_num(GS_CHAINACTION) = 2
        // sc_start(src, src, SC_QD_SHOT_READY, 100, target->id, skill_get_time(RL_QD_SHOT,1)).
        _sc?.Start(sd, StatusType.QdShotReady, val1: (int)target.Id, 0, 0, 0, durationMs: 1500, sd);
    }

    /// <summary>
    /// DAMAGE_DIV_FIX (battle.cpp:4365): a positive div multiplies the per-hit
    /// damage by the count and flags DMG_MULTI_HIT, preserving the critical
    /// animation when the swing crit.
    /// </summary>
    private static void ApplyMultiHit(BattleDamage result, int div)
    {
        result.Hits = div;
        result.Damage *= div;
        result.Type = result.IsCritical
            ? Core.Server.Packets.Out.ZC.DamageActionType.MultiHitCrit
            : Core.Server.Packets.Out.ZC.DamageActionType.MultiHit;
    }

    /// <summary>
    /// COMBAT-18 — one weapon-hand's damage through the renewal pipeline
    /// (battle.cpp:7635 body, steps 3-7): base damage → bAtkRate → size-fix →
    /// element fix → defense reduction (incl. SC_SIGNUMCRUCIS) → weapon mastery
    /// → cardfix(<paramref name="leftHand"/>) → caster-side SC weapon bumps →
    /// floor 1. Shared by the right hand and the dual-wield left hand so the two
    /// hands can never drift. The per-hand weapon fields
    /// (<paramref name="atkMin"/>/<paramref name="atkMax"/>/
    /// <paramref name="weaponLevel"/>/<paramref name="weaponType"/>/
    /// <paramref name="weaponElement"/>) come from rhw for the right hand and lhw
    /// for the left.
    /// </summary>
    private long ComputeHandDamage(
        Entity source, Entity target, BattleStats s, BattleStats t,
        bool srcIsPc, bool isCritical, bool forceMaxRoll,
        int atkMin, int atkMax, byte weaponLevel, int weaponType, byte weaponElement, bool leftHand)
    {
        long damage = CalcBaseDamage(atkMin, atkMax, s.Dex, weaponLevel, s.Batk, isCritical, forceMaxRoll, srcIsPc);

        // COMBAT-06: bAtkRate (SP_ATK_RATE) scales base weapon damage. rAthena
        // ATK_ADDRATE applies it to both hands (battle.cpp:4604).
        if (srcIsPc && (source as PlayerEntity)?.EquipBonuses is { AtkRate: var ar } && ar != 0)
            damage = damage * (100 + ar) / 100;

        // Size-fix (battle.cpp:2514) — PCs only; the per-hand weapon type drives
        // the renewal Knuckle/Whip ×Large penalty.
        if (srcIsPc)
            damage = damage * SizeMod(weaponType, t.Size) / 100;

        // Element fix (battle.cpp:453 battle_attr_fix) — per-hand weapon element.
        var atkEle = weaponElement == 0 ? BattleElement.Neutral : (BattleElement)weaponElement;
        damage = damage * ElementTable.GetRate(atkEle, t.DefenseElement, t.ElementLevel) / 100;
        if (damage < 0) damage = 0;

        // Defense reduction (battle.cpp:6843, renewal SIMPLE branch):
        // Damage = Damage * (4000 + eDEF) / (4000 + 10*eDEF) - sDEF.
        int def1 = t.Def;
        int vitDef = t.Def2;
        // SC_SIGNUMCRUCIS (status.cpp:11296) — Val2% def cut vs Undead/Demon.
        if (_sc != null && (t.Race == Map.Server.Status.BattleRace.Undead || t.Race == Map.Server.Status.BattleRace.Demon))
        {
            var signum = _sc.Get(target, Map.Server.Status.StatusType.Signumcrucis);
            if (signum != null && signum.Val2 > 0)
            {
                def1 -= def1 * signum.Val2 / 100;
                vitDef -= vitDef * signum.Val2 / 100;
            }
        }
        // COMBAT-43 — bIgnoreDefRace / bIgnoreDefClass: skip the hard+soft DEF
        // subtract vs the target's race/class (rAthena battle.cpp:3379). Per hand,
        // so the right and left weapon are evaluated independently here.
        if (srcIsPc && (source as PlayerEntity)?.EquipBonuses is { } eqDef
            && (((int)t.Race >= 0 && (eqDef.IgnoreDefRace & (1 << (int)t.Race)) != 0)
                || (eqDef.IgnoreDefClass & (1 << TargetClassBit(target))) != 0))
        {
            def1 = 0;
            vitDef = 0;
        }
        if (def1 == -400) def1 = -399; // div-by-zero guard from rAthena
        damage = damage * (4000L + def1) / (4000L + 10L * def1) - vitDef;

        // COMBAT-61 — P.ATK trait stat (battle.cpp:7775). rAthena applies
        // `wd.damage = floor(wd.damage * (100 + patk) / 100)` per hand to the
        // accumulator sum *before* the mastery add, PC sources only (the whole
        // patk/crit block is under `if (sd)`). Off-hand uses the same source
        // patk. Integer math mirrors rAthena's `(int64)floor` over int64.
        if (srcIsPc && s.Patk != 0)
            damage = damage * (100 + s.Patk) / 100;

        // Weapon mastery (battle.cpp:2215 battle_addmastery — bonus only).
        // COMBAT-40: pass the per-hand weapon type so the dual-wield off-hand
        // resolves its mastery from the LEFT weapon (rAthena weapontype2).
        if (_cards != null && source is PlayerEntity pcAtk)
            damage += _cards.AddMastery(pcAtk, target, damage, BattleAttackType.Weapon, weaponType);

        // Card fix (battle.cpp:711 battle_calc_cardfix) — left/right per hand.
        if (_cards != null)
            damage = _cards.CalcCardFix(BattleAttackType.Weapon, source, target, damage, leftHand);

        // Caster-side weapon-damage SC bumps. rAthena ATK_ADDRATE applies these
        // to both hands; mirror that here so the dual-wield off-hand benefits.
        if (_sc != null)
        {
            // SC_HEAT_BARREL (status.cpp:11392) — 5*Val1 % atk bonus.
            var hb = _sc.Get(source, Map.Server.Status.StatusType.HeatBarrel);
            if (hb != null && hb.Val1 > 0) damage += damage * (5 * hb.Val1) / 100;

            // SC_EDP (status.cpp:10522) — Val3 % (default 50+50*Val1).
            var edp = _sc.Get(source, Map.Server.Status.StatusType.Edp);
            if (edp != null)
            {
                var pct = edp.Val3 > 0 ? edp.Val3 : (50 + 50 * edp.Val1);
                if (pct > 0) damage += damage * pct / 100;
            }

            // SC__BLOODYLUST — Val2 % weapon-damage boost.
            var bl = _sc.Get(source, Map.Server.Status.StatusType.Bloodylust);
            if (bl != null && bl.Val2 > 0) damage += damage * bl.Val2 / 100;

            // SC_RUSHWINDMILL — Val2 % band weapon-damage boost.
            var rwm = _sc.Get(source, Map.Server.Status.StatusType.Rushwindmill);
            if (rwm != null && rwm.Val2 > 0) damage += damage * rwm.Val2 / 100;

            // SC_PYROCLASTIC — Val2 flat atk bonus.
            var pyro = _sc.Get(source, Map.Server.Status.StatusType.Pyroclastic);
            if (pyro != null && pyro.Val2 > 0) damage += pyro.Val2;
        }

        // COMBAT-61/77 — Res trait-stat physical reduction (battle.cpp:7820-7846):
        // `damage = damage * (5000 + res) / (5000 + 10*res)`, applied per hand when the
        // target's Res > 0. Unlike patk/crit this sits OUTSIDE the `if (sd)` block in
        // rAthena, so it reduces mob attacks too (no srcIsPc gate).
        if (t.Res > 0 && damage > 0)
        {
            // COMBAT-77 — first lower the EFFECTIVE res by the attacker's res-ignore %:
            // ignore_res_by_race[targetRace] + ignore_res_by_race[RC_ALL]
            //   + SC_A_TELUM.val2 + SC_POTENT_VENOM.val2, clamped to max_res_mres_ignored.
            int res = t.Res;
            int ignoreRes = 0;
            if (srcIsPc && (source as PlayerEntity)?.EquipBonuses is { IgnoreResRace: var irr })
            {
                var raceIdx = (int)t.Race;
                if (raceIdx >= 0 && raceIdx < irr.Length) ignoreRes += irr[raceIdx];
                ignoreRes += irr[(int)Map.Server.Status.BattleRace.All];
            }
            if (_sc != null)
            {
                var telum = _sc.Get(source, Map.Server.Status.StatusType.ATelum);
                if (telum != null && telum.Val2 > 0) ignoreRes += telum.Val2;
                var venom = _sc.Get(source, Map.Server.Status.StatusType.PotentVenom);
                if (venom != null && venom.Val2 > 0) ignoreRes += venom.Val2;
            }
            // battle_config.max_res_mres_ignored (config/battle/player.json, default 50).
            if (ignoreRes > MaxResMresIgnored) ignoreRes = MaxResMresIgnored;
            if (ignoreRes > 0) res -= res * ignoreRes / 100;
            damage = damage * (5000L + res) / (5000L + 10L * res);
        }

        // Floor to 1 (battle_min_damage).
        if (damage < 1) damage = 1;
        return damage;
    }

    /// <summary>
    /// COMBAT-18 — <c>battle_calc_attack_left_right_hands</c> (battle.cpp:7150).
    /// Mob sources keep their single <paramref name="damage"/>. For a PC:
    ///   * Katar normal attack → off-hand <paramref name="damage2"/> =
    ///     <c>damage * (1 + 2*TF_DOUBLE_lv) / 100</c> (Katar has no real off-hand
    ///     weapon, so any pre-computed damage2 is replaced).
    ///   * Dual-wield (a real off-hand weapon, <paramref name="damage2"/> &gt; 0):
    ///     the right hand is reduced to <c>(50 + 10*AS_RIGHT) %</c> (thief) /
    ///     <c>(70 + 10*KO_RIGHT) %</c> (kagerou/oboro) when a main-hand weapon is
    ///     present, and the left hand to <c>(30 + 10*AS_LEFT) %</c> (thief) /
    ///     <c>(50 + 10*KO_LEFT) %</c> (kagerou/oboro). Both floor at 1.
    /// </summary>
    private void ApplyLeftRightSplit(Entity source, int rightWeaponType, ref long damage, ref long damage2)
    {
        if (source is not PlayerEntity sd) return;
        var ids = sd.LearnedSkills;
        int Lv(ushort id) => ids.TryGetValue(id, out var l) ? l : 0;
        bool thief = Map.Server.Entities.MapidClass.IsBase(sd.ClassMask, Map.Server.Entities.MapidClass.Thief);
        bool kagerou = sd.ClassMask == Map.Server.Entities.MapidClass.KagerouOboro;

        // Katar: off-hand damage only on normal attacks (battle.cpp:7157).
        if (rightWeaponType == Map.Server.Inventory.WeaponTypeCodes.Katar)
        {
            int tfd = Lv(Map.Server.Skills.SkillIds.TF_DOUBLE);
            damage2 = damage * (1 + tfd * 2) / 100;
            return;
        }

        // Dual-wield: only adjusts when a real off-hand weapon produced damage2.
        if (damage2 <= 0) return;

        // is_attack_right_handed is false only when bare-handed main + off-hand
        // weapon (battle.cpp:2903); then the right swing isn't mastery-reduced.
        bool rightHanded = !Map.Server.Inventory.WeaponTypeCodes.IsFist(rightWeaponType);
        if (rightHanded && damage > 0)
        {
            if (thief) damage = damage * (50 + Lv(Map.Server.Skills.SkillIds.AS_RIGHT) * 10) / 100;
            else if (kagerou) damage = damage * (70 + Lv(Map.Server.Skills.SkillIds.KO_RIGHT) * 10) / 100;
            if (damage < 1) damage = 1;
        }

        if (thief) damage2 = damage2 * (30 + Lv(Map.Server.Skills.SkillIds.AS_LEFT) * 10) / 100;
        else if (kagerou) damage2 = damage2 * (50 + Lv(Map.Server.Skills.SkillIds.KO_LEFT) * 10) / 100;
        if (damage2 < 1) damage2 = 1;
    }

    /// <summary>
    /// Port of <c>is_attack_critical</c> (battle.cpp:2948) trimmed to the
    /// always-present branch: cri ≠ 0, COMBAT-21 adds the attacker's per-race
    /// <c>critaddrace</c> (battle.cpp:2980, stored ×10), subtract
    /// <c>2 × target.luk</c> (or <c>3 × target.luk</c> when defender is PC and
    /// attacker is non-PC), roll vs 1000 (cri is stored ×10).
    /// </summary>
    private bool TryCritical(Entity source, Entity target, BattleStats s, BattleStats t, bool srcIsPc, bool tgtIsPc)
    {
        int cri = s.Cri;
        // COMBAT-21 — bonus2 bCriticalAddRace: +crit chance vs a race (×10,
        // battle.cpp:2980). Added BEFORE the cri≤0 gate so a 0-base-crit attacker
        // can still crit a race it carries the card for (rAthena has no early-out).
        if (srcIsPc && (source as PlayerEntity)?.EquipBonuses is { } ab)
        {
            int tRace = (int)t.Race;
            if (tRace >= 0 && tRace < ab.CritAddRace.Length) cri += ab.CritAddRace[tRace];
            cri += ab.CritAddRace[(int)Map.Server.Status.BattleRace.All];
        }
        if (cri <= 0) return false;
        int lukMult = (!srcIsPc && tgtIsPc) ? 3 : 2;
        cri -= t.Luk * lukMult;
        if (cri <= 0) return false;
        return _rng.Next(1000) < cri;
    }

    /// <summary>
    /// Port of <c>battle_calc_base_damage</c> (battle.cpp:2453) for the
    /// renewal non-PC and PC standard paths. Picks min/max from weapon
    /// atk, rolls between them, adds <c>batk</c>, applies the +40% crit
    /// modifier from rAthena's <c>#ifdef RENEWAL</c> tail.
    /// </summary>
    private long CalcBaseDamage(int rawAtkMin, int atkMax, int dex, byte weaponLevel, ushort batk,
        bool isCritical, bool forceMaxRoll, bool isPc)
    {
        // PC: DEX-derived atkmin (battle.cpp:2453). Mob: rhw.atk straight.
        int atkMin = isPc ? ComputePcAtkMin(dex, weaponLevel, atkMax) : rawAtkMin;
        if (atkMin > atkMax) atkMin = atkMax;

        // P0.3 — Maximize Power / critical hits use atkMax. The
        // SC_MAXIMIZEPOWER (Whitesmith) flag forces every swing to
        // max roll matching rAthena's status_calc_*_atk fast path.
        long dmg = (isCritical || forceMaxRoll)
            ? atkMax
            : (atkMax > atkMin ? _rng.Next(atkMin, atkMax + 1) : atkMin);

        dmg += batk;

        // Renewal crit bonus — battle.cpp:2566
        if (isCritical) dmg = dmg * 14 / 10;
        return dmg;
    }

    /// <summary>
    /// rAthena <c>battle_calc_base_damage</c> PC <c>atkmin</c> (battle.cpp:2453):
    /// starts at DEX; when a weapon is equipped (<paramref name="weaponLevel"/>
    /// 1-5) it becomes <c>dex * (80 + weaponLv*20) / 100</c>; bare-handed
    /// (level 0) keeps DEX. Capped at <paramref name="atkMax"/> (= rhw.atk).
    /// </summary>
    internal static int ComputePcAtkMin(int dex, int weaponLevel, int atkMax)
    {
        int atkMin = dex;
        if (weaponLevel >= 1) atkMin = atkMin * (80 + weaponLevel * 20) / 100;
        return atkMin > atkMax ? atkMax : atkMin;
    }

    /// <summary>
    /// COMBAT-16 — renewal <c>db/re/size_fix.yml</c> weapon size penalty
    /// (<c>atkmods[t_size]</c>, battle.cpp:2453). In renewal the ONLY non-100
    /// entries are Knuckle and Whip, each 75% vs a Large target; every other
    /// weapon/size (and bare hand) is 100%. Mob attackers skip the lookup
    /// entirely (battle.cpp:2514) — this is only reached on the PC path.
    /// </summary>
    internal static int SizeMod(int weaponType, BattleSize targetSize)
        => (targetSize == BattleSize.Large && Map.Server.Inventory.WeaponTypeCodes.IsKnuckleOrWhip(weaponType))
            ? 75
            : 100;

    /// <summary>
    /// COMBAT-20 — rAthena <c>is_infinite_defense</c> (battle.cpp:2823) for the
    /// "plant" clamp: a target whose mode ignores the incoming attack lane takes
    /// only 1 damage. Melee/ranged split on <paramref name="isShortRange"/>
    /// (MD_IGNOREMELEE vs MD_IGNORERANGED); magic/misc gate on MD_IGNOREMAGIC /
    /// MD_IGNOREMISC. PCs (mode None) are never plants.
    /// </summary>
    internal static bool IsInfiniteDefense(Entity target, BattleAttackType lane, bool isShortRange,
        IStatusChangeService? sc = null)
    {
        // COMBAT-42 — SC_INVINCIBLE is universal: the target takes 1 from any lane
        // (rAthena is_infinite_defense, battle.cpp:2844).
        if (sc?.Get(target, StatusType.Invincible) != null) return true;
        var mode = target.Stats.Mode;
        return lane switch
        {
            BattleAttackType.Weapon => isShortRange
                ? (mode & MobMode.IgnoreMelee) != 0
                : (mode & MobMode.IgnoreRanged) != 0,
            BattleAttackType.Magic => (mode & MobMode.IgnoreMagic) != 0,
            BattleAttackType.Misc => (mode & MobMode.IgnoreMisc) != 0,
            _ => false,
        };
    }

    /// <summary>
    /// COMBAT-20 — final-stage plant clamp + GvG/BG scaling, applied AFTER the
    /// full damage is known. rAthena keeps the two mutually exclusive
    /// (battle_calc_weapon_attack: infinite-defense → <c>battle_calc_attack_plant</c>
    /// returns before <c>battle_calc_attack_gvg_bg</c>): a plant in WoE still
    /// takes exactly 1, no GvG rate. <paramref name="isSkill"/> selects the
    /// per-lane vs short/long zone rate.
    /// </summary>
    internal static void ApplyPlantAndZone(
        BattleDamage wd, Entity src, Entity target, bool isShortRange, bool isSkill, IZoneDamageService? zone,
        IStatusChangeService? sc = null, ushort skillId = 0)
    {
        if (IsInfiniteDefense(target, wd.Lane, isShortRange, sc))
        {
            // Plants take 1 (battle.cpp:7082). Right hand: 1 when it hit at all.
            if (wd.DidHit || wd.Damage > 0) wd.Damage = 1;
            if (wd.Damage2 > 0)
            {
                // Katar off-hand deals 0 vs plants; any other off-hand weapon 1.
                bool katar = src is PlayerEntity p
                    && p.WeaponType == Map.Server.Inventory.WeaponTypeCodes.Katar;
                wd.Damage2 = katar ? 0 : 1;
            }
            return;
        }

        if (zone != null)
        {
            wd.Damage = zone.Scale(wd.Lane, src, target, wd.Damage, isSkill, isShortRange, skillId);
            if (wd.Damage2 > 0)
                wd.Damage2 = zone.Scale(wd.Lane, src, target, wd.Damage2, isSkill, isShortRange, skillId);
        }
    }

    /// <summary>
    /// rAthena <c>battle_range_type</c> short/long boundary for auto-attacks —
    /// melee weapons (range ≤ 3) are BF_SHORT, bows/guns BF_LONG.
    /// </summary>
    internal static bool IsShortRange(Entity src) => src.Stats.AttackRange <= 3;

    /// <summary>
    /// COMBAT-43 — the target's class bit for <c>bIgnoreDefClass</c> (rAthena
    /// <c>status_get_class_</c>): a boss-protocol mob (<c>MD_STATUSIMMUNE</c>) is
    /// <see cref="Map.Server.Inventory.BattleClassFlag.Boss"/>; everything else is
    /// <see cref="Map.Server.Inventory.BattleClassFlag.Normal"/>. (Guardian is a WoE
    /// distinction tracked separately.)
    /// </summary>
    private static int TargetClassBit(Entity target)
        => (target.Stats.Mode & MobMode.StatusImmune) != 0
            ? (int)Map.Server.Inventory.BattleClassFlag.Boss
            : (int)Map.Server.Inventory.BattleClassFlag.Normal;

    /// <inheritdoc cref="IBattleCalculator.ApplyWeaponSkillPlantZone"/>
    public long ApplyWeaponSkillPlantZone(Entity src, Entity target, long damage, bool isShortRange, ushort skillId = 0)
    {
        if (damage <= 0) return damage;
        var wd = new BattleDamage { Lane = BattleAttackType.Weapon, Damage = damage };
        ApplyPlantAndZone(wd, src, target, isShortRange, isSkill: true, _zone, _sc, skillId);
        return wd.Damage;
    }

    /// <summary>
    /// Wave 67 / Track C — rAthena <c>battle_calc_magic_attack</c>
    /// centralised. Mirrors the magic chain end-to-end:
    /// (MatkMin+MatkMax)/2 baseline → SC_MAGICPOWER / SC_MOONLITSERENADE
    /// caster bumps → element table → MDEF/MDEF2 reduction → cardfix.
    /// </summary>
    public BattleDamage CalcMagicAttack(Entity source, Entity target, ushort skillId, ushort skillLevel, int ratePerLevel, long constantAddition = 0)
    {
        var result = new BattleDamage { Hits = 1, Lane = BattleAttackType.Magic };
        var s = source.Stats;
        var t = target.Stats;

        // Base magic damage (renewal: average of MatkMin / MatkMax).
        long baseDmg = (s.MatkMin + s.MatkMax) / 2;
        long damage = Math.Max(1, baseDmg * Math.Max(1, ratePerLevel) / 100);

        // Caster SC bumps (formerly in SkillAttackService.CalcMagicDamage).
        if (_sc != null)
        {
            // SC_MAGICPOWER (status.cpp:10556-10564) — next cast +5% per Val1
            // then SC ends.
            var mp = _sc.Get(source, StatusType.Magicpower);
            if (mp != null && mp.Val1 > 0)
            {
                damage = damage * (100 + 5 * mp.Val1) / 100;
                _sc.End(source, StatusType.Magicpower);
            }
            // SC_MOONLITSERENADE — Val2 % Matk for the Wanderer band.
            var mls = _sc.Get(source, StatusType.Moonlitserenade);
            if (mls != null && mls.Val2 > 0)
                damage += damage * mls.Val2 / 100;
        }

        // COMBAT-06: bMatkRate (SP_MATK_RATE) — % MATK scaling from equip/cards.
        if ((source as PlayerEntity)?.EquipBonuses is { MatkRate: var mr } && mr != 0)
            damage = damage * (100 + mr) / 100;

        // COMBAT-03/56: RE_LVL_DMOD (config/const.hpp) — renewal base-level magic
        // scaling above level 99, but ONLY for arms that use the macro. A magic skill
        // whose battle_calc_attack_skill_ratio arm omits it (ReLvlDmodOmit) stays flat.
        if (source.Level > 99 && !Map.Server.Skills.ReLvlDmodOmit.OmitsRatioScaling(skillId))
            damage = damage * source.Level / 100;

        // COMBAT-12: ATK_ADD — the plugin's CalculateSkillConstantAddition
        // (rAthena battle_calc_skill_constant_addition, battle.cpp:6606), added
        // after the ratio and before the element/MDEF fix. 0 for nearly all
        // renewal magic (the pre-renewal GS_MAGICALBULLET case is the exception).
        if (constantAddition > 0)
            damage += constantAddition;

        // COMBAT-19 — element from the skill (battle_get_magic_element,
        // battle.cpp:3582): skill_db element, with ELE_WEAPON → weapon element,
        // ELE_ENDOWED → endow, ELE_RANDOM → random. Falls back to the caster's
        // weapon element when no resolver is wired.
        var atkEle = _elements?.GetMagicElement(source, skillId, skillLevel)
            ?? (s.WeaponElement == 0 ? BattleElement.Neutral : (BattleElement)s.WeaponElement);
        damage = damage * ElementTable.GetRate(atkEle, t.DefenseElement, t.ElementLevel) / 100;
        if (damage < 0) damage = 0;

        // MDEF reduction (renewal: dmg * (1000+10*mdef) / (1000+10*(mdef+mdef2)))
        // — falls back to simple sub when mdef stats are 0.
        int mdef = t.Mdef;
        int mdef2 = t.Mdef2;
        if (mdef > 0 || mdef2 > 0)
        {
            damage = damage * (1000L + 10L * mdef) / (1000L + 10L * (mdef + mdef2));
            damage -= mdef2;
        }

        // COMBAT-22 — bonus2 bSkillAtk: per-skill magic % (pc_skillatk_bonus,
        // battle.cpp:9263). Applied after MDEF, before cardfix.
        if ((source as PlayerEntity)?.EquipBonuses.SkillAtk is { } satk
            && satk.TryGetValue(skillId, out var sapct) && sapct != 0)
            damage += damage * sapct / 100;

        // COMBAT-64 — bonus2 bSubSkill: the DEFENDER's per-skill incoming-damage reduction
        // (pc_sub_skillatk_bonus, battle.cpp:7873 — ATK_ADDRATE(-i)), symmetric to SkillAtk.
        if ((target as PlayerEntity)?.EquipBonuses.SubSkillAtk is { } ssatk
            && ssatk.TryGetValue(skillId, out var sspct) && sspct != 0)
            damage -= damage * sspct / 100;

        // Card fix (per-target race/element/size/class additions). COMBAT-19:
        // pass the resolved skill element so the defender's bSubEle lookup uses
        // the magic element, not the caster's weapon element.
        if (_cards != null)
            damage = _cards.CalcCardFix(BattleAttackType.Magic, source, target, damage, leftHand: false, attackElement: atkEle);

        if (damage < 1) damage = 1;
        result.Damage = damage;
        // COMBAT-20 — plant clamp (MD_IGNOREMAGIC → 1) + GvG/BG magic rate. Magic
        // is always a skill (BF_SKILL) so the per-lane gvg/bg_magic rate applies.
        ApplyPlantAndZone(result, source, target, isShortRange: false, isSkill: true, _zone, _sc, skillId);
        PopulateMotionFields(result, t);
        return result;
    }

    /// <summary>
    /// Wave 67 / Track C — rAthena <c>battle_calc_misc_attack</c>
    /// (battle.cpp:8540) centralised. Misc skills (traps, item-based
    /// damage, mercenary blessing-on-undead) scale with caster
    /// (level + int) × skill damage rate; no def subtract, element
    /// table applied via the misc atk element column.
    /// </summary>
    public BattleDamage CalcMiscAttack(Entity source, Entity target, ushort skillId, ushort skillLevel, int ratePerLevel)
    {
        var result = new BattleDamage { Hits = 1, Lane = BattleAttackType.Misc };
        var s = source.Stats;
        var t = target.Stats;

        long baseDmg = source.Level + s.IntStat;
        long damage = Math.Max(1, baseDmg * Math.Max(1, ratePerLevel) / 100);

        // COMBAT-03/56: RE_LVL_MDMOD (standard misc variant) — base-level scaling
        // above 99, but ONLY for arms that use the macro. A misc skill whose
        // battle_calc_misc_attack arm omits it (ReLvlDmodOmit.OmitsMiscScaling) stays
        // flat. Ranger traps use the RE_LVL_TMDMOD variant in TrapDamage (COMBAT-55).
        if (source.Level > 99 && !Map.Server.Skills.ReLvlDmodOmit.OmitsMiscScaling(skillId))
            damage = damage * source.Level / 100;

        // COMBAT-19 — element from the skill (battle_get_misc_element,
        // battle.cpp:3675): skill_db element, with ELE_WEAPON / ELE_ENDOWED →
        // Neutral and ELE_RANDOM → random.
        var atkEle = _elements?.GetMiscElement(source, skillId, skillLevel)
            ?? (s.WeaponElement == 0 ? BattleElement.Neutral : (BattleElement)s.WeaponElement);
        damage = damage * ElementTable.GetRate(atkEle, t.DefenseElement, t.ElementLevel) / 100;
        if (damage < 0) damage = 0;

        if (_cards != null)
            damage = _cards.CalcCardFix(BattleAttackType.Misc, source, target, damage, leftHand: false, attackElement: atkEle);

        if (damage < 1) damage = 1;
        result.Damage = damage;
        // COMBAT-20 — plant clamp (MD_IGNOREMISC → 1) + GvG/BG misc rate.
        ApplyPlantAndZone(result, source, target, isShortRange: false, isSkill: true, _zone, _sc, skillId);
        PopulateMotionFields(result, t);
        return result;
    }
}
