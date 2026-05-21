using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HAMI_DEFENCE — Amistr Defense. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_defense.cpp</c>.
/// Applies SC_DEFENCE to both target (master) and self (homunculus).
/// </summary>
public sealed class Defense : SkillImpl
{
    public Defense() : base(SkillIds.HAMI_DEFENCE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Defence, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Sc?.Start(src, StatusType.Defence, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
