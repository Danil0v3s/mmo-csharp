using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_SV_ROOTTWIST — Summoner Silvervine Root Twist. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/silvervineroottwist.cpp</c>.
/// Refuses Boss-mode targets. Applies SC_SV_ROOTTWIST; scheduled +1000 ms
/// damage tick via SU_SV_ROOTTWIST_ATK is TODO.
/// </summary>
public sealed class SilvervineRootTwist : SkillImpl
{
    public SilvervineRootTwist() : base(SkillIds.SU_SV_ROOTTWIST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Sc?.Start(target, StatusType.SvRoottwist, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
