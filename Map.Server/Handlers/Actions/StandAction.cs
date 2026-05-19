using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Handlers.Actions;

/// <summary>rAthena DMG_STAND_UP (action=3) — pc_setstand.</summary>
public sealed class StandAction : IActionHandler
{
    public byte ActionCode => 3;

    private readonly IAttackService _attack;
    private readonly IVisibilityService _visibility;

    public StandAction(IAttackService attack, IVisibilityService visibility)
    {
        _attack = attack;
        _visibility = visibility;
    }

    public void Apply(PlayerEntity player, int targetId)
    {
        _attack.StopAttack(player);
        if (!player.IsSitting) return;
        player.IsSitting = false;
        _visibility.SendToArea(player, new ZC_NOTIFY_ACT3
        {
            SourceId = player.Id.Value,
            TargetId = 0,
            ServerTick = (uint)Environment.TickCount,
            ActionType = DamageActionType.Stand,
            Div = 0,
        });
    }
}
