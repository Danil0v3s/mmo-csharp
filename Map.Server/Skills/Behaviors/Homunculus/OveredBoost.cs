using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_OVERED_BOOST — Homunculus Overed Boost. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_overedboost.cpp</c>.
/// Applies SC_OVERED_BOOST to master + self.
/// </summary>
public sealed class OveredBoost : SkillImpl
{
    public OveredBoost() : base(SkillIds.MH_OVERED_BOOST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.OveredBoost, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Sc?.Start(src, StatusType.OveredBoost, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
