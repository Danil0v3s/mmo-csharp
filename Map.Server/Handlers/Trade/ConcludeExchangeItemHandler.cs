using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Trade;

namespace Map.Server.Handlers.Trade;

/// <summary>
/// rAthena <c>clif_parse_TradeOk</c> (clif.cpp:12529). Locks this
/// side's offer; partner sees a "Who pressed OK" notification.
/// </summary>
[PacketHandler(PacketHeader.CZ_CONCLUDE_EXCHANGE_ITEM)]
public class ConcludeExchangeItemHandler(
    IEntityRegistry registry,
    ITradeService trade,
    ISessionManagerAccessor sessions
) : IPacketHandler<MapSessionData, CZ_CONCLUDE_EXCHANGE_ITEM>
{
    public Task HandleAsync(MapSessionData session, CZ_CONCLUDE_EXCHANGE_ITEM packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity side
            || session.Trade is not { } state)
        {
            return Task.CompletedTask;
        }
        var partnerSession = sessions.GetByEntityId(new EntityId(state.PartnerCharId));

        if (trade.Ok(side) == TradeOpResult.Ok)
        {
            TradeNotifier.NotifyOk(session, partnerSession);
        }
        return Task.CompletedTask;
    }
}
