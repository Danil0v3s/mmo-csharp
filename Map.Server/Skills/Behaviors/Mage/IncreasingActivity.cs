using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_INCREASING_ACTIVITY — Elemental Master Increasing Activity.
/// AP (Activity Point) restore: <c>10 * skillLevel</c> AP on PC targets.
/// AP is a 4th-class trait stat — we approximate with SP for now.
/// </summary>
public sealed class IncreasingActivity : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;
    public IncreasingActivity() : base(SkillIds.EM_INCREASING_ACTIVITY) { }
    public IncreasingActivity(IStatusOpsService? statusOps = null) : base(SkillIds.EM_INCREASING_ACTIVITY) => _statusOps = statusOps;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId,
                    Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: AP heal (Activity Point trait); SP-heal placeholder.
        _statusOps?.Heal(target, 0, 10 * skillLevel, 0);
    }
}
