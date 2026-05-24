using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_ESTIMATION — Wizard Sense. Manual port of
/// <c>rathena-fork/src/map/skills/mage/sense.cpp</c>.
///
/// <para>Opens the monster-info panel (HP / element / size / race /
/// drop list). Fails on player targets. The actual info-panel packet
/// (<c>clif_skill_estimation</c>) isn't ported here yet — for now we
/// land the broadcast frame.</para>
/// </summary>
public sealed class Sense : SkillImpl
{
    public Sense() : base(SkillIds.WZ_ESTIMATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        if (target is PlayerEntity)
        {
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillEstimation(sd, target);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
