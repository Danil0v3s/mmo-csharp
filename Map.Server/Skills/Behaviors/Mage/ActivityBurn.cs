using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_ACTIVITY_BURN — Elemental Master Activity Burn. AP burn on PC
/// targets at <c>(20 + 10*lv) %</c> chance. AP isn't surfaced —
/// SP burn placeholder per lv: 20/30/50/60/70.
/// </summary>
public sealed class ActivityBurn : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;
    private static readonly int[] ApBurn = { 20, 30, 50, 60, 70 };

    public ActivityBurn() : base(SkillIds.EM_ACTIVITY_BURN) { }
    public ActivityBurn(IStatusOpsService? statusOps = null) : base(SkillIds.EM_ACTIVITY_BURN) => _statusOps = statusOps;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is PlayerEntity && Random.Shared.Next(100) < 20 + 10 * skillLevel)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            int burn = ApBurn[Math.Clamp(skillLevel - 1, 0, ApBurn.Length - 1)];
            // status_zap(target, 0, 0, ap_burn) — AP burn approximated as SP zap.
            if (target is PlayerEntity dst)
            {
                dst.Sp = Math.Max(0, dst.Sp - burn);
            }
        }
        else if (src is PlayerEntity sd)
        {
            ctx.Client?.BroadcastSkillFail(sd, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
        }
    }
}
