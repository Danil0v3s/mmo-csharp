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

    public long CalcCardFix(BattleAttackType attackType, Entity src, Entity target, long damage, bool leftHand)
    {
        if (damage == 0) return 0;

        // Card-fix only applies when the source is a player (mobs don't
        // wear cards). The bundle is recomputed by
        // EquipBonusAggregator.BuildBundle on every equip change.
        if (src is not PlayerEntity pc) return damage;

        var bundle = pc.EquipBonuses;
        if (bundle == null) return damage;

        var ts = target.Stats;
        // Renewal MULT base = 100 (%); each bonus column adds onto
        // the multiplier additively, then we apply once.
        long mult = 100;

        // Race attack bonus (indexed by target race + RC_All slot).
        var raceIdx = (int)ts.Race;
        if (raceIdx >= 0 && raceIdx < bundle.AddRace.Length) mult += bundle.AddRace[raceIdx];
        mult += bundle.AddRace[(int)BattleRace.All];

        // Element attack bonus (indexed by target defense element).
        var eleIdx = (int)ts.DefenseElement;
        if (eleIdx >= 0 && eleIdx < bundle.AddEle.Length) mult += bundle.AddEle[eleIdx];
        mult += bundle.AddEle[(int)BattleElement.All];

        // Size attack bonus (indexed by target size).
        var sizeIdx = (int)ts.Size;
        if (sizeIdx >= 0 && sizeIdx < bundle.AddSize.Length) mult += bundle.AddSize[sizeIdx];
        mult += bundle.AddSize[(int)BattleSize.All];

        // Class attack bonus — rAthena maps MD_MVP → CLASS_BOSS;
        // everything else CLASS_NORMAL. CLASS_GUARDIAN exists on
        // rAthena but our MobMode flags don't carry the guardian
        // bit yet, so guardians collapse to Normal (a future patch
        // adds the bit when GvG castles port).
        var classIdx = (int)Inventory.BattleClassFlag.Normal;
        if ((ts.Mode & MobMode.Mvp) != 0) classIdx = (int)Inventory.BattleClassFlag.Boss;
        if (classIdx >= 0 && classIdx < bundle.AddClass.Length) mult += bundle.AddClass[classIdx];
        mult += bundle.AddClass[(int)Inventory.BattleClassFlag.All];

        // Attack-range bonus — short = melee (range ≤ 2), long = ranged.
        if (pc.Stats.AttackRange > 2) mult += bundle.LongAtkRate;
        else mult += bundle.ShortAtkRate;

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
