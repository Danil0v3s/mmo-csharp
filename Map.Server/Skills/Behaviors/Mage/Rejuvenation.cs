using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SA_FULLRECOVERY — Sage Full Recovery. 100 % HP + SP heal (status-immune blocks).</summary>
public sealed class Rejuvenation : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;
    public Rejuvenation() : base(SkillIds.SA_FULLRECOVERY) { }
    public Rejuvenation(IStatusOpsService? statusOps = null) : base(SkillIds.SA_FULLRECOVERY) => _statusOps = statusOps;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0) return;
        _statusOps?.PercentHeal(target, 100, 100);
    }
}
