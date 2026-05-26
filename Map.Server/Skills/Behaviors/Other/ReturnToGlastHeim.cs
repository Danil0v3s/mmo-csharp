using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_GLASTHEIM_RECALL — Return to Glast Heim. Manual port of
/// <c>rathena-fork/src/map/skills/other/returntoglastheim.cpp</c>.
/// Teleports the caster to glast_01 (200, 268).
/// </summary>
public sealed class ReturnToGlastHeim : SkillImpl
{
    public ReturnToGlastHeim() : base(SkillIds.ALL_GLASTHEIM_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Setpos?.Setpos(pc, "glast_01", x: 200, y: 268);
    }
}
