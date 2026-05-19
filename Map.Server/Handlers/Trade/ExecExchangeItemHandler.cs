using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Trade;

namespace Map.Server.Handlers.Trade;

/// <summary>
/// rAthena <c>clif_parse_TradeCommit</c>. Commit fires once both
/// sides have pressed Trade — the second caller drives the swap +
/// success packets, the first just waits.
/// </summary>
[PacketHandler(PacketHeader.CZ_EXEC_EXCHANGE_ITEM)]
public class ExecExchangeItemHandler(
    IEntityRegistry registry,
    ITradeService trade,
    ISessionManagerAccessor sessions,
    ILogger<ExecExchangeItemHandler> logger
) : IPacketHandler<MapSessionData, CZ_EXEC_EXCHANGE_ITEM>
{
    public Task HandleAsync(MapSessionData session, CZ_EXEC_EXCHANGE_ITEM packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity side)
        {
            return Task.CompletedTask;
        }

        // Snapshot partner before Commit clears state.
        MapSessionData? partnerSession = null;
        if (session.Trade is { } state)
        {
            partnerSession = sessions.GetByEntityId(new EntityId(state.PartnerCharId));
        }

        var result = trade.Commit(side);
        switch (result)
        {
            case TradeOpResult.Ok:
                TradeNotifier.NotifyCompleted(session, partnerSession, 0);
                break;
            case TradeOpResult.InvalidStage:
                // Waiting for the other side to press Trade — no packets,
                // partner's eventual Exec will fire both completes.
                break;
            default:
                // Validation failure during commit — service already
                // cleared the trade. Tell both sides it failed.
                logger.LogWarning("Trade commit failed for char {Char}: {Reason}",
                    side.CharacterId, result);
                TradeNotifier.NotifyCompleted(session, partnerSession, 1);
                break;
        }
        return Task.CompletedTask;
    }
}
