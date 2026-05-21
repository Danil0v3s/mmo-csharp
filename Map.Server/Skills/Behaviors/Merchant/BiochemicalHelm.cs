using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_CP_HELM — Alchemist Chemical Protection: Helm. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/biochemicalhelm.cpp</c>.
/// Player-only target; head-equip gate TODO.
/// </summary>
public sealed class BiochemicalHelm : SkillImpl
{
    public BiochemicalHelm() : base(SkillIds.AM_CP_HELM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is PlayerEntity && target is not PlayerEntity)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Sc?.Start(target, StatusType.CpHelm, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
