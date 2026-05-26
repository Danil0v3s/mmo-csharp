using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// RETURN_TO_ELDICASTES — Return to Eldicastes. Port of
/// <c>rathena-fork/src/map/skills/other/returntoeldicastes.cpp</c>.
/// Teleports the caster to dicastes01 (198, 187).
/// </summary>
public sealed class ReturnToEldicastes : SkillImpl
{
    public ReturnToEldicastes() : base(SkillIds.RETURN_TO_ELDICASTES) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Setpos?.Setpos(pc, "dicastes01", x: 198, y: 187);
    }
}
