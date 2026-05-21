using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_GLASTHEIM_RECALL — Return to Glast Heim. Manual port of
/// <c>rathena-fork/src/map/skills/other/returntoglastheim.cpp</c>.
/// Teleports to glast_01 (200, 268). pc_setpos is TODO.
/// </summary>
public sealed class ReturnToGlastHeim : SkillImpl
{
    public ReturnToGlastHeim() : base(SkillIds.ALL_GLASTHEIM_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
