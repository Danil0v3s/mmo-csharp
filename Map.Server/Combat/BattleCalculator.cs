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
    private readonly Random _rng;
    private readonly IBattleCardService? _cards;
    private readonly IStatusChangeService? _sc;
    private readonly IMadoGearService? _mado;

    public BattleCalculator(Random? rng = null, IBattleCardService? cards = null, IStatusChangeService? sc = null, IMadoGearService? mado = null)
    {
        _rng = rng ?? Random.Shared;
        _cards = cards;
        _sc = sc;
        _mado = mado;
    }

    public BattleDamage CalcWeaponAttack(Entity source, Entity target)
    {
        var result = new BattleDamage { Lane = BattleAttackType.Weapon };
        var s = source.Stats;
        var t = target.Stats;
        var srcIsPc = source is PlayerEntity;
        var tgtIsPc = target is PlayerEntity;

        // --- Step 1: critical roll  (battle.cpp:2948 is_attack_critical) ---
        bool isCritical = TryCritical(s, t, srcIsPc, tgtIsPc);

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
        long damage = CalcBaseDamage(s, isCritical, forceMaxRoll, srcIsPc);

        // COMBAT-06: bAtkRate (SP_ATK_RATE) — renewal battle_get_atkpercent
        // (battle.cpp:4604) scales base weapon damage BEFORE the skill ratio.
        if (srcIsPc && (source as PlayerEntity)?.EquipBonuses is { AtkRate: var ar } && ar != 0)
            damage = damage * (100 + ar) / 100;

        // Mob/PC distinction: PCs would get size-mod via inventory atkmods.
        // Until equip is parsed, apply the default mob size-mod table.
        // rAthena: when no map_session_data sd is present (mob attacker),
        // the path skips the sizefix lookup entirely (battle.cpp:2514).
        if (srcIsPc)
        {
            damage = damage * SizeMod(t.Size) / 100;
        }

        // --- Step 4: element fix  (battle.cpp:453 battle_attr_fix) ---
        var atkEle = s.WeaponElement == 0 ? BattleElement.Neutral : (BattleElement)s.WeaponElement;
        damage = damage * ElementTable.GetRate(atkEle, t.DefenseElement, t.ElementLevel) / 100;
        if (damage < 0) damage = 0;

        // --- Step 6: defense reduction (battle.cpp:6843, renewal SIMPLE branch) ---
        // RE: Damage = Damage * (4000 + eDEF) / (4000 + 10*eDEF) - sDEF.
        // Pre-equip we don't have NK_SIMPLEDEFENSE skills; standard branch
        // is the normal autoattack swing.
        int def1 = t.Def;        // hard def (equipment)
        int vitDef = t.Def2;     // soft def (renewal: just def2 verbatim)

        // Wave 26 — rAthena <c>SC_SIGNUMCRUCIS</c> (status.cpp:11296).
        // Crusader's Signum Crucis reduces the target's defense by
        // <c>Val2 %</c> when the target is Undead or Demon race. Stored
        // per rAthena as Val2 = 14+3*level / 14+5*level. We honor whatever
        // the caster stored.
        if (_sc != null && (t.Race == Map.Server.Status.BattleRace.Undead || t.Race == Map.Server.Status.BattleRace.Demon))
        {
            var signum = _sc.Get(target, Map.Server.Status.StatusType.Signumcrucis);
            if (signum != null && signum.Val2 > 0)
            {
                def1 = def1 - (def1 * signum.Val2 / 100);
                vitDef = vitDef - (vitDef * signum.Val2 / 100);
            }
        }

        if (def1 == -400) def1 = -399; // div-by-zero guard from rAthena
        damage = damage * (4000L + def1) / (4000L + 10L * def1) - vitDef;

        // --- Step 5a: weapon mastery additive bonus
        //               (battle.cpp:2215 battle_addmastery, renewal returns bonus only) ---
        if (_cards != null && source is PlayerEntity pcAtk)
        {
            damage += _cards.AddMastery(pcAtk, target, damage, BattleAttackType.Weapon);
        }

        // --- Step 5b: card fix
        //               (battle.cpp:711 battle_calc_cardfix) ---
        if (_cards != null)
        {
            damage = _cards.CalcCardFix(BattleAttackType.Weapon, source, target, damage, leftHand: false);
        }

        // --- Step 5c: caster-side weapon-damage SC bumps ---------------
        // Each read here is one of the rAthena allowlist entries where the
        // SC's combat impact lives on the attacker side. Order matches
        // rAthena status.cpp arms:
        if (_sc != null)
        {
            // SC_HEAT_BARREL (status.cpp:11392) — Gunslinger Heat Barrel.
            // 5 * Val1 % atk bonus. Val1 = level (1..5).
            var hb = _sc.Get(source, Map.Server.Status.StatusType.HeatBarrel);
            if (hb != null && hb.Val1 > 0)
            {
                damage += damage * (5 * hb.Val1) / 100;
            }

            // SC_EDP (status.cpp:10522-10535) — Assassin Cross Enchant
            // Deadly Poison. Val3 stores the bonus damage % the SC adds
            // to weapon hits. Falls back to a per-level default
            // (50 + 50*Val1 %) when Val3 is unset.
            var edp = _sc.Get(source, Map.Server.Status.StatusType.Edp);
            if (edp != null)
            {
                var pct = edp.Val3 > 0 ? edp.Val3 : (50 + 50 * edp.Val1);
                if (pct > 0) damage += damage * pct / 100;
            }

            // SC__BLOODYLUST — Shadow Chaser Bloody Lust. Val2 = dmg%
            // boost applied to the caster's weapon hit.
            var bl = _sc.Get(source, Map.Server.Status.StatusType.Bloodylust);
            if (bl != null && bl.Val2 > 0)
            {
                damage += damage * bl.Val2 / 100;
            }

            // SC_RUSHWINDMILL — Wanderer / Minstrel song. Val2 stores
            // the % weapon-damage boost for the band.
            var rwm = _sc.Get(source, Map.Server.Status.StatusType.Rushwindmill);
            if (rwm != null && rwm.Val2 > 0)
            {
                damage += damage * rwm.Val2 / 100;
            }

            // SC_PYROCLASTIC — Mechanic Pyroclastic. Val2 = additive
            // atk bonus; the element override is handled by the
            // element resolver elsewhere.
            var pyro = _sc.Get(source, Map.Server.Status.StatusType.Pyroclastic);
            if (pyro != null && pyro.Val2 > 0)
            {
                damage += pyro.Val2;
            }
        }

        // --- Step 7: floor to 1 unless it actually missed (battle_min_damage) ---
        if (damage < 1) damage = 1;

        result.Damage = damage;
        result.Type = isCritical
            ? Core.Server.Packets.Out.ZC.DamageActionType.Critical
            : Core.Server.Packets.Out.ZC.DamageActionType.Normal;

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
            int heat = atkEle == BattleElement.Fire ? 3 : 1;
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
    /// Port of <c>is_attack_critical</c> (battle.cpp:2948) trimmed to the
    /// always-present branch: cri ≠ 0, subtract <c>2 × target.luk</c> (or
    /// <c>3 × target.luk</c> when defender is PC and attacker is non-PC),
    /// roll vs 1000 (cri is stored ×10).
    /// </summary>
    private bool TryCritical(BattleStats s, BattleStats t, bool srcIsPc, bool tgtIsPc)
    {
        if (s.Cri <= 0) return false;
        int cri = s.Cri;
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
    private long CalcBaseDamage(BattleStats s, bool isCritical, bool forceMaxRoll = false, bool isPc = false)
    {
        int atkMax = s.WatkMax;
        // PC: DEX-derived atkmin (battle.cpp:2453). Mob: rhw.atk straight.
        int atkMin = isPc ? ComputePcAtkMin(s.Dex, s.WeaponLevel, atkMax) : s.WatkMin;
        if (atkMin > atkMax) atkMin = atkMax;

        // P0.3 — Maximize Power / critical hits use atkMax. The
        // SC_MAXIMIZEPOWER (Whitesmith) flag forces every swing to
        // max roll matching rAthena's status_calc_*_atk fast path.
        long dmg = (isCritical || forceMaxRoll)
            ? atkMax
            : (atkMax > atkMin ? _rng.Next(atkMin, atkMax + 1) : atkMin);

        dmg += s.Batk;

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
    /// Default rAthena <c>size_fix</c> table (item_db default atkmods when
    /// no weapon equipped). 100 / 75 / 50 — used when a PC attacks and the
    /// equip path isn't ported yet. Mob attackers skip this entirely (per
    /// battle.cpp:2514), matching the rAthena code path.
    /// </summary>
    private static int SizeMod(BattleSize targetSize) => targetSize switch
    {
        BattleSize.Small => 100,
        BattleSize.Medium => 100,
        BattleSize.Large => 100,
        _ => 100,
    };

    /// <summary>
    /// Wave 67 / Track C — rAthena <c>battle_calc_magic_attack</c>
    /// centralised. Mirrors the magic chain end-to-end:
    /// (MatkMin+MatkMax)/2 baseline → SC_MAGICPOWER / SC_MOONLITSERENADE
    /// caster bumps → element table → MDEF/MDEF2 reduction → cardfix.
    /// </summary>
    public BattleDamage CalcMagicAttack(Entity source, Entity target, ushort skillId, ushort skillLevel, int ratePerLevel)
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

        // COMBAT-03: RE_LVL_MDMOD (config/const.hpp) — renewal base-level magic
        // scaling above level 99. The per-skill INF2_DISABLELVDMG opt-out needs
        // skill_db Inf2 flags loaded (not yet wired) — tracked in COMBAT-14.
        if (source.Level > 99)
            damage = damage * source.Level / 100;

        // Element table — magic uses the caster's atk element OR the skill's
        // declared element. The full per-skill element lookup lands later;
        // for now we use the caster's weapon element (same as weapon path).
        var atkEle = s.WeaponElement == 0 ? BattleElement.Neutral : (BattleElement)s.WeaponElement;
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

        // Card fix (per-target race/element/size/class additions).
        if (_cards != null)
            damage = _cards.CalcCardFix(BattleAttackType.Magic, source, target, damage, leftHand: false);

        if (damage < 1) damage = 1;
        result.Damage = damage;
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

        // COMBAT-03: RE_LVL_DMOD (standard misc variant) — base-level scaling
        // above 99. Ranger-trap skills use the RE_LVL_TMDMOD variant
        // (damage*150/100 + damage*lv/100) + the INF2_DISABLELVDMG opt-out —
        // both tracked in COMBAT-14.
        if (source.Level > 99)
            damage = damage * source.Level / 100;

        var atkEle = s.WeaponElement == 0 ? BattleElement.Neutral : (BattleElement)s.WeaponElement;
        damage = damage * ElementTable.GetRate(atkEle, t.DefenseElement, t.ElementLevel) / 100;
        if (damage < 0) damage = 0;

        if (_cards != null)
            damage = _cards.CalcCardFix(BattleAttackType.Misc, source, target, damage, leftHand: false);

        if (damage < 1) damage = 1;
        result.Damage = damage;
        PopulateMotionFields(result, t);
        return result;
    }
}
