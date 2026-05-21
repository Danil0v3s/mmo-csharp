using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_INTENSIVE_AIM — Night Watch Intensive Aim toggle. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/intensiveaim.cpp</c>.
/// Toggles SC_INTENSIVE_AIM and ends SC_INTENSIVE_AIM_COUNT on the caster.
/// </summary>
public sealed class IntensiveAim : SkillImpl
{
    public IntensiveAim() : base(SkillIds.NW_INTENSIVE_AIM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(src, StatusType.IntensiveAimCount);
        if (ctx.Sc?.Get(target, StatusType.IntensiveAim) != null)
            ctx.Sc.End(target, StatusType.IntensiveAim);
        else
            ctx.Sc?.Start(target, StatusType.IntensiveAim, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
