using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Handlers.Actions;

/// <summary>
/// rAthena DMG_SIT_DOWN (action=2) — pc_setsit. Cancels attack, flips
/// IsSitting, broadcasts ZC_NOTIFY_ACT3 with action=Sit to AOI.
/// </summary>
public sealed class SitAction : IActionHandler
{
    public byte ActionCode => 2;

    private readonly IAttackService _attack;
    private readonly IVisibilityService _visibility;

    public SitAction(IAttackService attack, IVisibilityService visibility)
    {
        _attack = attack;
        _visibility = visibility;
    }

    public void Apply(PlayerEntity player, int targetId)
    {
        _attack.StopAttack(player);
        if (player.IsSitting) return;
        player.IsSitting = true;
        _visibility.SendToArea(player, new ZC_NOTIFY_ACT3
        {
            SourceId = player.Id.Value,
            TargetId = 0,
            ServerTick = (uint)Environment.TickCount,
            ActionType = DamageActionType.Sit,
            Div = 0,
        });
    }
}
