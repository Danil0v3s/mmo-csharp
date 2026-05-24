using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_INCREASING_ACTIVITY — Elemental Master Increasing Activity
/// (skill.cpp:EM_INCREASING_ACTIVITY arm). Restores <c>10 * skillLevel</c>
/// AP on PC targets. AP (Activity Point) is the 4th-class trait pool
/// living on <see cref="PlayerEntity.Ap"/> / <see cref="PlayerEntity.MaxAp"/>;
/// the mutation goes straight on the entity to match the rAthena
/// <c>status_heal(bl, 0, 0, ap)</c> third-argument pattern.
/// </summary>
public sealed class IncreasingActivity : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;
    public IncreasingActivity() : base(SkillIds.EM_INCREASING_ACTIVITY) { }
    public IncreasingActivity(IStatusOpsService? statusOps = null) : base(SkillIds.EM_INCREASING_ACTIVITY) => _statusOps = statusOps;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity tpc)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId,
                    Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        tpc.Ap = Math.Min(tpc.MaxAp, tpc.Ap + 10 * skillLevel);
    }
}
