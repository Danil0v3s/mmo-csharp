using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_POWEROFFLOCK — Summoner Power of Flock. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/powerofflock.cpp</c>.
/// Applies SC_FEAR + SC_FREEZE to splash targets. Splash dispatch is
/// TODO; we apply to the named target.
/// </summary>
public sealed class PowerofFlock : SkillImpl
{
    public PowerofFlock() : base(SkillIds.SU_POWEROFFLOCK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Fear, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
        ctx.Sc?.Start(target, StatusType.Freeze, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
