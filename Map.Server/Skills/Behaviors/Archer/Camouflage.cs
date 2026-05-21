using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_CAMOUFLAGE — Ranger Camouflage. Manual port of
/// <c>rathena-fork/src/map/skills/archer/camouflage.cpp</c>.
/// Toggle SC_CAMOUFLAGE on the target.
/// </summary>
public sealed class Camouflage : SkillImpl
{
    public Camouflage() : base(SkillIds.RA_CAMOUFLAGE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null && ctx.Sc.End(target, StatusType.Camouflage))
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            return;
        }
        ctx.Sc?.Start(target, StatusType.Camouflage, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
