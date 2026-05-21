using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HLIF_AVOID — Lif Avoid. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_avoid.cpp</c>.
/// Applies SC_AVOID to both target (master) and self (homunculus).
/// </summary>
public sealed class Avoid : SkillImpl
{
    public Avoid() : base(SkillIds.HLIF_AVOID) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Avoid, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(src, StatusType.Avoid, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
