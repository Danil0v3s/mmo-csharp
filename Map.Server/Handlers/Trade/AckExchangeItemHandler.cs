using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Trade;

namespace Map.Server.Handlers.Trade;

/// <summary>
/// rAthena <c>clif_parse_TradeAck</c> (clif.cpp:12500). Client sends:
/// 3 = accept, 4 = cancel/decline. On accept, both sides see the
/// ack=accept and can start adding items.
/// </summary>
[PacketHandler(PacketHeader.CZ_ACK_EXCHANGE_ITEM)]
public class AckExchangeItemHandler(
    IEntityRegistry registry,
    ITradeService trade,
    ISessionManagerAccessor sessions,
    ILogger<AckExchangeItemHandler> logger
) : IPacketHandler<MapSessionData, CZ_ACK_EXCHANGE_ITEM>
{
    public Task HandleAsync(MapSessionData session, CZ_ACK_EXCHANGE_ITEM packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity target)
        {
            return Task.CompletedTask;
        }

        var accept = packet.Result == 3;
        // Look up the partner BEFORE Acknowledge clears the trade
        // state when accept=false.
        MapSessionData? partnerSession = null;
        if (session.Trade is { } state)
        {
            partnerSession = sessions.GetByEntityId(new EntityId(state.PartnerCharId));
        }

        var result = trade.Acknowledge(target, accept);
        if (!accept)
        {
            // Decline: both sides see "cancel" (4).
            TradeNotifier.NotifyAckTo(session, 4);
            TradeNotifier.NotifyAckTo(partnerSession, 4);
            return Task.CompletedTask;
        }

        if (result == TradeOpResult.Ok)
        {
            // Both sides open the trade window.
            TradeNotifier.NotifyAckTo(session, 3);
            TradeNotifier.NotifyAckTo(partnerSession, 3);
        }
        else
        {
            // Accept-but-fail (target session vanished etc.).
            TradeNotifier.NotifyAckTo(session, TradeNotifier.AckCodeFor(result));
            logger.LogDebug("Trade ack failed for char {Char}: {Reason}", target.CharacterId, result);
        }
        return Task.CompletedTask;
    }
}
