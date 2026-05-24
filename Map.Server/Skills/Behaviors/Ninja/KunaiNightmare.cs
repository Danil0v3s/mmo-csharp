using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_HITOUAKUMU — Kunai Nightmare. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/kunainightmare.cpp</c>.
/// Splash damage; ratio <c>+(-100 + 18000) + 5*pow</c>, +50% extra if
/// target has SC_NIGHTMARE. Ends SC_NIGHTMARE on hit.
/// </summary>
public sealed class KunaiNightmare : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public KunaiNightmare() : base(SkillIds.SS_HITOUAKUMU) { }

    public KunaiNightmare(ISkillAttackService? skillAttack = null) : base(SkillIds.SS_HITOUAKUMU)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 18000) + 5 * src.Stats.Pow;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena map_foreachinrange( skill_area_sub … BCT_ENEMY | SD_SPLASH | 1 ).
        (_skillAttack ?? ctx.SkillAttack)?.SkillAttackArea(src, target, SkillId, skillLevel);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.End(target, StatusType.Nightmare);
}
