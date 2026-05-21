using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_LIGHTHALZEN_RECALL — Return to Lighthalzen. Manual port of
/// <c>rathena-fork/src/map/skills/other/returntolighthalzen.cpp</c>.
/// Teleports to lighthalzen (307, 307). pc_setpos is TODO.
/// </summary>
public sealed class ReturnToLighthalzen : SkillImpl
{
    public ReturnToLighthalzen() : base(SkillIds.ALL_LIGHTHALZEN_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
