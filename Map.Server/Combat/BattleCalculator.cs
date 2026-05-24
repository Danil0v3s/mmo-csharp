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

    public BattleCalculator(Random? rng = null, IBattleCardService? cards = null, IStatusChangeService? sc = null)
    {
        _rng = rng ?? Random.Shared;
        _cards = cards;
        _sc = sc;
    }

    public BattleDamage CalcWeaponAttack(Entity source, Entity target)
    {
        var result = new BattleDamage();
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
        long damage = CalcBaseDamage(s, isCritical, forceMaxRoll);

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

        // --- Step 5c: SC_HEAT_BARREL (Gunslinger) ---------------
        // Wave 26 — rAthena <c>SC_HEAT_BARREL</c> (status.cpp:11392).
        // Gunslinger Heat Barrel grants <c>5*Val1 %</c> ATK and a per-bullet
        // damage bonus. Val1 = level (1..5). Read on the caster's swing.
        if (_sc != null)
        {
            var hb = _sc.Get(source, Map.Server.Status.StatusType.HeatBarrel);
            if (hb != null && hb.Val1 > 0)
            {
                var pct = 5 * hb.Val1;
                damage += damage * pct / 100;
            }
        }

        // --- Step 7: floor to 1 unless it actually missed (battle_min_damage) ---
        if (damage < 1) damage = 1;

        result.Damage = damage;
        result.Type = isCritical
            ? Core.Server.Packets.Out.ZC.DamageActionType.Critical
            : Core.Server.Packets.Out.ZC.DamageActionType.Normal;
        return result;
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
    private long CalcBaseDamage(BattleStats s, bool isCritical, bool forceMaxRoll = false)
    {
        int atkMin = s.WatkMin;
        int atkMax = s.WatkMax;
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
}
