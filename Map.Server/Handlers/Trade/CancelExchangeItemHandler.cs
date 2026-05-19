using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Trade;

namespace Map.Server.Handlers.Trade;

/// <summary>rAthena <c>clif_parse_TradeCancel</c> (clif.cpp:12538).</summary>
[PacketHandler(PacketHeader.CZ_CANCEL_EXCHANGE_ITEM)]
public class CancelExchangeItemHandler(
    IEntityRegistry registry,
    ITradeService trade,
    ISessionManagerAccessor sessions
) : IPacketHandler<MapSessionData, CZ_CANCEL_EXCHANGE_ITEM>
{
    public Task HandleAsync(MapSessionData session, CZ_CANCEL_EXCHANGE_ITEM packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity side)
        {
            return Task.CompletedTask;
        }

        // Look up partner BEFORE Cancel clears the trade.
        MapSessionData? partnerSession = null;
        if (session.Trade is { } state)
        {
            partnerSession = sessions.GetByEntityId(new EntityId(state.PartnerCharId));
        }

        trade.Cancel(side);
        TradeNotifier.NotifyCancel(session, partnerSession);
        return Task.CompletedTask;
    }
}
