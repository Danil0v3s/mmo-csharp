using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_DEADHILLHERE — Minstrel/Wanderer Valley of Death (Dead Hill
/// Here). Manual port of <c>rathena-fork/src/map/skills/archer/valleyofdeath.cpp</c>.
///
/// <para>Revives a dead player with current SP swapped into HP and
/// the SP value reduced by <c>(60 - 10*lv) %</c>. Player-only target;
/// no-op on living targets. Death check + SP-swap math not surfaced
/// to this hook — for now we just call Revive with a token HP%.</para>
/// </summary>
public sealed class ValleyOfDeath : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public ValleyOfDeath() : base(SkillIds.WM_DEADHILLHERE) { }

    public ValleyOfDeath(IStatusOpsService? statusOps = null) : base(SkillIds.WM_DEADHILLHERE)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity) return;
        // rAthena: HP = max(SP, 1); SP -= SP * (60-10*lv)/100. Token revive at 20 % HP for now.
        _statusOps?.Revive(target, percentHp: 20, percentSp: 0);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
