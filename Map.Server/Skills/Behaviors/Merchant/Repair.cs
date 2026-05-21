using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_REPAIR — Mechanic Repair. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/repair.cpp</c>.
/// Heals <c>4/7/13/17/23 %</c> max HP of a player target (madogear
/// check TODO).
/// </summary>
public sealed class Repair : SkillImpl
{
    private static readonly int[] Pct = { 0, 4, 7, 13, 17, 23 };
    private readonly IStatusOpsService? _statusOps;

    public Repair() : base(SkillIds.NC_REPAIR) { }

    public Repair(IStatusOpsService? statusOps = null) : base(SkillIds.NC_REPAIR)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity) return;
        var pct = skillLevel < Pct.Length ? Pct[skillLevel] : 23;
        var heal = target.Stats.MaxHp * pct / 100;
        _statusOps?.Heal(target, heal, 0, 2);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
