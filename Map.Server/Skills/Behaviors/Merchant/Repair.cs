using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_REPAIR — Mechanic Repair. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/repair.cpp</c>.
/// Player + Madogear-only target. Heals <c>4/7/13/17/23 %</c> max HP
/// at lv 1..5. Targets without OPTION_MADOGEAR fail with
/// <see cref="Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail"/>.
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
        // rAthena: dstsd must be a Madogear-mounted player.
        if (target is not PlayerEntity dst || (dst.Option & PlayerOption.Madogear) == 0)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        var pct = skillLevel < Pct.Length ? Pct[skillLevel] : 23;
        var heal = target.Stats.MaxHp * pct / 100;
        _statusOps?.Heal(target, heal, 0, 2);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
