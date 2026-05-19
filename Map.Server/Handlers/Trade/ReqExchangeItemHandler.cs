using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Trade;

namespace Map.Server.Handlers.Trade;

/// <summary>
/// rAthena <c>clif_parse_TradeRequest</c> (clif.cpp:12465). The initiator's
/// client sends the target's account id; we resolve to a PlayerEntity and
/// kick off the trade service. On success, the target sees a confirm
/// popup; on failure the initiator gets the right error code back.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_EXCHANGE_ITEM)]
public class ReqExchangeItemHandler(
    IEntityRegistry registry,
    ITradeService trade,
    ISessionManagerAccessor sessions,
    ILogger<ReqExchangeItemHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_EXCHANGE_ITEM>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_EXCHANGE_ITEM packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity initiator)
        {
            return Task.CompletedTask;
        }

        var targetSession = sessions.GetByAccountId(packet.TargetAccountId);
        if (targetSession?.EntityId is not { } targetEid
            || registry.Get(targetEid) is not PlayerEntity target)
        {
            TradeNotifier.NotifyAckTo(session, TradeNotifier.AckCodeFor(TradeOpResult.TargetNotExist));
            return Task.CompletedTask;
        }

        var result = trade.Request(initiator, target);
        if (result == TradeOpResult.Ok)
        {
            TradeNotifier.NotifyRequest(targetSession, initiator);
        }
        else
        {
            TradeNotifier.NotifyAckTo(session, TradeNotifier.AckCodeFor(result));
            logger.LogDebug(
                "Trade request rejected: char {Char} → acc {Target}: {Reason}",
                initiator.CharacterId, packet.TargetAccountId, result);
        }
        return Task.CompletedTask;
    }
}
