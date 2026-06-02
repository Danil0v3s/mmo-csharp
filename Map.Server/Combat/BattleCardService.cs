using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IBattleCardService"/>.
///
/// <para><b>CalcCardFix</b>: applies the attacker's percent damage
/// modifiers (race / element / size / class) via the aggregator on
/// <c>BattleStats</c>. Until the aggregator gains those fields the
/// method is a documented pass-through — it returns the input
/// damage unchanged. The canonical entry exists so the damage
/// pipeline doesn't need to be rewritten when the aggregator lands.
/// rAthena reference: battle.cpp:711.</para>
///
/// <para><b>AddMastery</b>: walks the attacker's
/// <see cref="PlayerEntity.LearnedSkills"/> for the rAthena
/// mastery skills and returns the additive bonus. Race / element
/// filters use the target's <c>BattleStats.Race</c> /
/// <c>DefenseElement</c>. rAthena reference: battle.cpp:2215.</para>
/// </summary>
public sealed class BattleCardService : IBattleCardService
{
    // rAthena skill ids (db/re/skill_db.yml). Hard-coded constants
    // here so the mastery lookup doesn't go through the skill_db on
    // every swing. Same approach we used in ShopService for
    // Discount/Overcharge.
    private const ushort AL_DEMONBANE     = 156;
    private const ushort HT_BEASTBANE     = 119;
    private const ushort BS_WEAPONRESEARCH = 122;
    private const ushort NC_RESEARCHFE    = 2502;
    private const ushort NC_MADOLICENCE   = 2501;
    private const ushort NV_BREAKTHROUGH  = 8000;
    private const ushort RA_RANGERMAIN    = 2351;

    private readonly ILogger<BattleCardService> _logger;
    public BattleCardService(ILogger<BattleCardService> logger) => _logger = logger;

    public long CalcCardFix(BattleAttackType attackType, Entity src, Entity target, long damage, bool leftHand,
        BattleElement? attackElement = null)
    {
        if (damage == 0) return 0;

        // rAthena battle_calc_cardfix (battle.cpp:711) folds BOTH the
        // attacker's offensive cards (Add*) AND the defender's defensive
        // cards (Sub*). Renewal adds onto a single percent multiplier; we
        // mirror that additive form. (The strict per-category multiplicative
        // APPLY_CARDFIX_RE grouping is COMBAT-21.)
        var ss = src.Stats;
        var ts = target.Stats;
        long mult = 100;

        // --- Attacker offensive cardfix (only a PC wears cards). Indexed by
        //     the TARGET's race / element / size / class. ---
        if (src is PlayerEntity pc && pc.EquipBonuses is { } ab)
        {
            var raceIdx = (int)ts.Race;
            if (raceIdx >= 0 && raceIdx < ab.AddRace.Length) mult += ab.AddRace[raceIdx];
            mult += ab.AddRace[(int)BattleRace.All];

            var eleIdx = (int)ts.DefenseElement;
            if (eleIdx >= 0 && eleIdx < ab.AddEle.Length) mult += ab.AddEle[eleIdx];
            mult += ab.AddEle[(int)BattleElement.All];

            var sizeIdx = (int)ts.Size;
            if (sizeIdx >= 0 && sizeIdx < ab.AddSize.Length) mult += ab.AddSize[sizeIdx];
            mult += ab.AddSize[(int)BattleSize.All];

            // rAthena maps MD_MVP → CLASS_BOSS, else CLASS_NORMAL (guardian
            // bit not modelled until GvG castles port).
            var tClass = (ts.Mode & MobMode.Mvp) != 0
                ? (int)Inventory.BattleClassFlag.Boss : (int)Inventory.BattleClassFlag.Normal;
            if (tClass >= 0 && tClass < ab.AddClass.Length) mult += ab.AddClass[tClass];
            mult += ab.AddClass[(int)Inventory.BattleClassFlag.All];

            // Range bonus — short = melee (range ≤ 2), long = ranged.
            mult += pc.Stats.AttackRange > 2 ? ab.LongAtkRate : ab.ShortAtkRate;
        }

        // --- Defender defensive cardfix (only a PC wears cards). Indexed by
        //     the ATTACKER's race / element / size / class; SUBTRACTS. This is
        //     what makes a player's bSubRace / bSubEle / bSubSize / bSubClass
        //     resist cards work — including against mob attackers, which the
        //     old `src is not PC → return` early-out skipped entirely. ---
        if (target is PlayerEntity tpc && tpc.EquipBonuses is { } db)
        {
            var aRace = (int)ss.Race;
            if (aRace >= 0 && aRace < db.SubRace.Length) mult -= db.SubRace[aRace];
            mult -= db.SubRace[(int)BattleRace.All];

            // Attacker's attack element. COMBAT-19: magic/misc pass the resolved
            // skill element via attackElement; weapon attacks (null) use the rh
            // weapon element (rh_ele).
            var aEle = (int)(attackElement ?? (BattleElement)ss.WeaponElement);
            if (aEle >= 0 && aEle < db.SubEle.Length) mult -= db.SubEle[aEle];
            mult -= db.SubEle[(int)BattleElement.All];

            var aSize = (int)ss.Size;
            if (aSize >= 0 && aSize < db.SubSize.Length) mult -= db.SubSize[aSize];

            var aClass = (ss.Mode & MobMode.Mvp) != 0
                ? (int)Inventory.BattleClassFlag.Boss : (int)Inventory.BattleClassFlag.Normal;
            if (aClass >= 0 && aClass < db.SubClass.Length) mult -= db.SubClass[aClass];
            mult -= db.SubClass[(int)Inventory.BattleClassFlag.All];
        }

        // Apply the % multiplier with floor-at-1.
        return Math.Max(1, damage * mult / 100);
    }

    public long AddMastery(PlayerEntity attacker, Entity target, long damage, BattleAttackType type)
    {
        // rAthena: renewal returns only the bonus (caller does the
        // addition); pre-renewal mutates damage. We follow renewal —
        // the result is the additive bonus only.
        long bonus = 0;
        var ts = target.Stats;

        var demonBane = attacker.LearnedSkills.GetValueOrDefault(AL_DEMONBANE);
        if (demonBane > 0 && target is MobEntity
            && (IsUndead(ts) || ts.Race == BattleRace.Demon))
        {
            // rAthena: skill * (3 + (level+1)*0.05). Mobs only.
            bonus += (long)(demonBane * (3 + (attacker.Level + 1) * 0.05));
        }

        var rangerMain = attacker.LearnedSkills.GetValueOrDefault(RA_RANGERMAIN);
        if (rangerMain > 0
            && (ts.Race == BattleRace.Brute || ts.Race == BattleRace.Plant
                || ts.Race == BattleRace.Fish || ts.Race == BattleRace.PlayerDoram))
        {
            bonus += rangerMain * 5;
        }

        var researchFe = attacker.LearnedSkills.GetValueOrDefault(NC_RESEARCHFE);
        if (researchFe > 0
            && (ts.DefenseElement == BattleElement.Fire || ts.DefenseElement == BattleElement.Earth))
        {
            bonus += researchFe * 10;
        }

        // Madogear License is an unconditional bonus.
        bonus += 15 * attacker.LearnedSkills.GetValueOrDefault(NC_MADOLICENCE);

        var beastBane = attacker.LearnedSkills.GetValueOrDefault(HT_BEASTBANE);
        if (beastBane > 0
            && (ts.Race == BattleRace.Insect || ts.Race == BattleRace.Brute
                || ts.Race == BattleRace.PlayerDoram))
        {
            bonus += beastBane * 4;
        }

        // Weapon Research applies to all weapons (renewal).
        bonus += attacker.LearnedSkills.GetValueOrDefault(BS_WEAPONRESEARCH) * 2;

        var breakthrough = attacker.LearnedSkills.GetValueOrDefault(NV_BREAKTHROUGH);
        if (breakthrough > 0)
        {
            bonus += 15 * breakthrough + (breakthrough > 4 ? 25 : 0);
        }

        // Kagerou/Oboro Spirit Charm — bonus when full charm stack
        // matches target's defense element opposite (Fire vs Earth,
        // Water vs Fire, Land vs Wind, Wind vs Water).
        if (attacker.SpiritCharm >= 10)
        {
            var t = (CharmType)attacker.SpiritCharmType;
            var de = ts.DefenseElement;
            if ((t == CharmType.Fire && de == BattleElement.Earth)
                || (t == CharmType.Water && de == BattleElement.Fire)
                || (t == CharmType.Land && de == BattleElement.Wind)
                || (t == CharmType.Wind && de == BattleElement.Water))
            {
                bonus += attacker.Stats.Str * 2; // rAthena: STR scaled
            }
        }

        return bonus;
    }

    private static bool IsUndead(Status.BattleStats s)
        => s.Race == BattleRace.Undead || s.DefenseElement == BattleElement.Undead;

    /// <summary>
    /// rAthena <c>battle_calc_chorusbonus</c> (battle.cpp:2847).
    /// Renewal path is hard <c>return 0</c> per the rAthena #ifdef
    /// guard — the chorus damage matrix is pre-renewal only. Our
    /// server is renewal, so this is structurally complete.
    /// Pre-renewal branch would count same-map party members with
    /// MAPID_THIRDMASK | MAPID_MINSTRELWANDERER and return 0 / (n-2) /
    /// 5 based on the rAthena thresholds.
    /// </summary>
    public int CalcChorusBonus(PlayerEntity attacker) => 0;
}

/// <summary>
/// Mirror of rAthena <c>e_charm_type</c> (status.hpp). Values
/// pinned to rAthena indices.
/// </summary>
public enum CharmType
{
    Water = 0,
    Land  = 1,
    Fire  = 2,
    Wind  = 3,
}
