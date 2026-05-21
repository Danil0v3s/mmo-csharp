using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ECLAGE_RECALL — Return to Eclage. Manual port of
/// <c>rathena-fork/src/map/skills/other/returntoeclage.cpp</c>.
/// Teleports the caster to eclage_in (47, 31). pc_setpos pipeline is TODO.
/// </summary>
public sealed class ReturnToEclage : SkillImpl
{
    public ReturnToEclage() : base(SkillIds.ECLAGE_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
