using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// RETURN_TO_ELDICASTES — Return to Eldicastes. Manual port of
/// <c>rathena-fork/src/map/skills/other/returntoeldicastes.cpp</c>.
/// Teleports to dicastes (198, 187). pc_setpos is TODO.
/// </summary>
public sealed class ReturnToEldicastes : SkillImpl
{
    public ReturnToEldicastes() : base(SkillIds.RETURN_TO_ELDICASTES) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
