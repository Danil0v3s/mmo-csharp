using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ECL_SNOWFLIP — Snow Flip cleanse. Manual port of
/// <c>rathena-fork/src/map/skills/other/snowflip.cpp</c>.
/// Cleanses Sleep / Bleeding / Burning / DeepSleep.
/// </summary>
public sealed class SnowFlip : SkillImpl
{
    public SnowFlip() : base(SkillIds.ECL_SNOWFLIP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Sleep);
        ctx.Sc?.End(target, StatusType.Bleeding);
        ctx.Sc?.End(target, StatusType.Burning);
        ctx.Sc?.End(target, StatusType.Deepsleep);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
