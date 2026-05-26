using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_THANATOS_RECALL — Return to Thanatos Tower. Port of
/// <c>rathena-fork/src/map/skills/other/returntothanatos.cpp</c>.
/// Teleports the caster to thana_t01 (139, 156).
/// </summary>
public sealed class ReturnToThanatos : SkillImpl
{
    public ReturnToThanatos() : base(SkillIds.ALL_THANATOS_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Setpos?.Setpos(pc, "thana_t01", x: 139, y: 156);
    }
}
