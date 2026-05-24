using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_ARBITRIUM — Cardinal Arbitrium. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/arbitrium.cpp</c>.
///
/// <para>Magic strike. Ratio: <c>-100 + 1000*lv + 10*SPL</c> + Fidus
/// Animus mastery bonus. On hit, applies Deep Silence (chance
/// <c>20 + 5*lv %</c>) and triggers a follow-up splash hit via
/// <c>CD_ARBITRIUM_ATK</c>. The ATK companion uses a higher ratio
/// (<c>-100 + 1750*lv</c>).</para>
///
/// <para>CD_FIDUS_ANIMUS mastery isn't on our SkillIds catalog yet
/// (Cardinal passive); we omit its contribution.</para>
/// </summary>
public sealed class Arbitrium : SkillImpl
{
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;
    private readonly Random _rng;

    public Arbitrium() : base(SkillIds.CD_ARBITRIUM) => _rng = Random.Shared;

    public Arbitrium(
        Map.Server.Skills.ISkillAttackService? skillAttack = null,
        Random? rng = null) : base(SkillIds.CD_ARBITRIUM)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 1000*lv + 10*SPL + (10*FidusAnimusLv*lv)
        return baseRatio + (-100 + 1000 * skillLevel) + 10 * src.Stats.Spl;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: SC_HANDICAPSTATE_DEEPSILENCE at (20+5*lv) % chance.
        var chance = 20 + 5 * skillLevel;
        if (_rng.Next(100) < chance)
        {
            ctx.Sc?.Start(target, StatusType.HandicapstateDeepsilence,
                val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
        }

        // Deferred per PARITY-REMAINING.md §P2.3: follow-up CD_ARBITRIUM_ATK splash
        // hit needs SkillBehaviorRegistry lookup mid-cast (not surfaced through
        // SkillBehaviorContext yet); rAthena calls skill_castend_damage_id(CD_ARBITRIUM_ATK, ...).
    }
}
