using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_THANATOS_RECALL — Return to Thanatos Tower. Manual port of
/// <c>rathena-fork/src/map/skills/other/returntothanatos.cpp</c>.
/// Teleports to thana_t01 (139, 156). pc_setpos is TODO.
/// </summary>
public sealed class ReturnToThanatos : SkillImpl
{
    public ReturnToThanatos() : base(SkillIds.ALL_THANATOS_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
