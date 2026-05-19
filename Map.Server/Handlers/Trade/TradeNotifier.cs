using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;
using Map.Server.Trade;

namespace Map.Server.Handlers.Trade;

/// <summary>
/// Helpers that translate trade-service outcomes into wire packets.
/// Kept separate from the service so the service stays pure (it owns
/// the gameplay invariant) and packet emission stays out of the unit
/// tests of the service.
///
/// Mirrors the per-call wire emissions in rAthena's
/// <c>clif_traderequest</c> / <c>clif_traderesponse</c> /
/// <c>clif_tradeadditem</c> / <c>clif_tradeitemok</c> /
/// <c>clif_tradedeal_lock</c> / <c>clif_tradecancelled</c> /
/// <c>clif_tradecompleted</c> (clif.cpp:4700-4850).
/// </summary>
public static class TradeNotifier
{
    /// <summary>Send the request popup to the target side.</summary>
    public static void NotifyRequest(MapSessionData targetSession, PlayerEntity initiator)
    {
        targetSession.EnqueuePacket(new ZC_REQ_EXCHANGE_ITEM
        {
            RequesterName = initiator.Name,
        });
    }

    /// <summary>Send a result code to <paramref name="session"/> (initiator on rejection / both on accept).</summary>
    public static void NotifyAckTo(MapSessionData? session, byte result)
    {
        if (session == null) return;
        session.EnqueuePacket(new ZC_ACK_EXCHANGE_ITEM { Result = result });
    }

    /// <summary>Notify partner that this side added an item — partner sees it appear.</summary>
    public static void NotifyAddItem(MapSessionData partnerSession, InventoryItem item, int amount)
    {
        partnerSession.EnqueuePacket(new ZC_ADD_EXCHANGE_ITEM
        {
            Amount = amount,
            NameId = (ushort)item.NameId,
            Identified = item.Identified ? (byte)1 : (byte)0,
            Damaged = item.Attribute,
            Refine = item.Refine,
            Card0 = (ushort)item.Card0,
            Card1 = (ushort)item.Card1,
            Card2 = (ushort)item.Card2,
            Card3 = (ushort)item.Card3,
        });
    }

    /// <summary>Notify partner the trade window got <paramref name="zeny"/> zeny added (rAthena uses nameid=0 / refine=0 for this).</summary>
    public static void NotifyAddZeny(MapSessionData partnerSession, long zeny)
    {
        partnerSession.EnqueuePacket(new ZC_ADD_EXCHANGE_ITEM
        {
            Amount = (int)Math.Min(zeny, int.MaxValue),
            NameId = 0,
        });
    }

    /// <summary>Ack the add to the sender (success / overweight / canceled).</summary>
    public static void NotifyAddAck(MapSessionData session, ushort clientIndex, byte result)
    {
        session.EnqueuePacket(new ZC_ACK_ADD_EXCHANGE_ITEM
        {
            Index = clientIndex,
            Result = result,
        });
    }

    /// <summary>Tell both sides that "self" or "partner" pressed OK.</summary>
    public static void NotifyOk(MapSessionData selfSession, MapSessionData? partnerSession)
    {
        selfSession.EnqueuePacket(new ZC_CONCLUDE_EXCHANGE_ITEM { Who = 0 });
        partnerSession?.EnqueuePacket(new ZC_CONCLUDE_EXCHANGE_ITEM { Who = 1 });
    }

    /// <summary>Tell both sides the trade is cancelled.</summary>
    public static void NotifyCancel(MapSessionData? selfSession, MapSessionData? partnerSession)
    {
        selfSession?.EnqueuePacket(new ZC_CANCEL_EXCHANGE_ITEM());
        partnerSession?.EnqueuePacket(new ZC_CANCEL_EXCHANGE_ITEM());
    }

    /// <summary>Tell both sides the trade succeeded (or failed).</summary>
    public static void NotifyCompleted(MapSessionData? a, MapSessionData? b, byte result)
    {
        a?.EnqueuePacket(new ZC_EXEC_EXCHANGE_ITEM { Result = result });
        b?.EnqueuePacket(new ZC_EXEC_EXCHANGE_ITEM { Result = result });
    }

    /// <summary>Translate <see cref="TradeOpResult"/> to the rAthena ZC_ACK_EXCHANGE_ITEM byte code.</summary>
    public static byte AckCodeFor(TradeOpResult result) => result switch
    {
        TradeOpResult.TargetTooFar => 0,
        TradeOpResult.TargetNotExist => 1,
        TradeOpResult.AlreadyTrading => 2,
        TradeOpResult.Ok => 3,
        _ => 2,
    };
}
