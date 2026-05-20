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
        // rAthena APPLY_CARDFIX is a 1000-based multiplier reduction.
        // Until BattleStats grows the indexed_bonus aggregator (race
        // / element / size / class arrays), there are no cards to
        // accumulate; return the input as-is. The pipeline still
        // routes through here so future aggregator wiring is a
        // single-site change.
        return damage;
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
