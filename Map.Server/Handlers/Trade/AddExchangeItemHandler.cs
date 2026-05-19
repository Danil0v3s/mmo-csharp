using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Trade;

namespace Map.Server.Handlers.Trade;

/// <summary>
/// rAthena <c>clif_parse_TradeAddItem</c> (clif.cpp:12511). Index 0
/// means add zeny; otherwise client_index → server slot = index − 2.
/// </summary>
[PacketHandler(PacketHeader.CZ_ADD_EXCHANGE_ITEM)]
public class AddExchangeItemHandler(
    IEntityRegistry registry,
    ITradeService trade,
    ISessionManagerAccessor sessions,
    ILogger<AddExchangeItemHandler> logger
) : IPacketHandler<MapSessionData, CZ_ADD_EXCHANGE_ITEM>
{
    public Task HandleAsync(MapSessionData session, CZ_ADD_EXCHANGE_ITEM packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity side
            || session.Trade is not { } state)
        {
            return Task.CompletedTask;
        }
        var partnerSession = sessions.GetByEntityId(new EntityId(state.PartnerCharId));

        if (packet.Index == 0)
        {
            // Zeny add path.
            var zenyResult = trade.AddZeny(side, packet.Amount);
            byte ackCode = zenyResult == TradeOpResult.Ok ? (byte)0
                : zenyResult == TradeOpResult.NotEnoughZeny ? (byte)1 // matches "overweight"-style fail
                : (byte)2;
            TradeNotifier.NotifyAddAck(session, 0, ackCode);
            if (zenyResult == TradeOpResult.Ok && partnerSession != null)
            {
                TradeNotifier.NotifyAddZeny(partnerSession, packet.Amount);
            }
            return Task.CompletedTask;
        }

        // Item add path. rAthena server_index = client_index − 2.
        var serverIndex = packet.Index - 2;
        var addResult = trade.AddItem(side, serverIndex, packet.Amount);
        byte itemAck = addResult == TradeOpResult.Ok ? (byte)0 : (byte)2;
        TradeNotifier.NotifyAddAck(session, packet.Index, itemAck);

        if (addResult == TradeOpResult.Ok && partnerSession != null && session.Inventory is { } inv
            && serverIndex >= 0 && serverIndex < inv.Count)
        {
            TradeNotifier.NotifyAddItem(partnerSession, inv[serverIndex], packet.Amount);
        }
        else if (addResult != TradeOpResult.Ok)
        {
            logger.LogDebug("Trade add rejected: char {Char} slot {Slot}: {Reason}",
                side.CharacterId, serverIndex, addResult);
        }
        return Task.CompletedTask;
    }
}
