using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_DREAM_SUMMERNIGHT — Summer Night's Dream emote. Manual port of
/// <c>rathena-fork/src/map/skills/other/summernightdream.cpp</c>.
/// Animation only.
/// </summary>
public sealed class SummerNightDream : SkillImpl
{
    public SummerNightDream() : base(SkillIds.ALL_DREAM_SUMMERNIGHT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
