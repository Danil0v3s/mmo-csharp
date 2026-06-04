using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Cash;

/// <summary>
/// Default <see cref="ICashShopClientService"/> — routes every cash-shop ZC packet to the owning
/// player's own session (all cash-shop packets are SELF-target in rAthena).
/// </summary>
public sealed class CashShopClientService : ICashShopClientService
{
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<CashShopClientService> _logger;

    public CashShopClientService(ISessionManagerAccessor sessions, ILogger<CashShopClientService> logger)
    {
        _sessions = sessions;
        _logger = logger;
    }

    public void SendOpen(PlayerEntity pc, int tab)
        => _sessions.GetByEntityId(pc.Id)?.EnqueuePacket(new ZC_SE_CASHSHOP_OPEN
        { CashPoints = pc.CashPoints, KafraPoints = pc.KafraPoints, Tab = tab });

    public void SendCatalog(PlayerEntity pc, IReadOnlyList<(int tab, IReadOnlyList<(uint itemId, int price)> items)> tabs)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session == null) return;
        foreach (var (tab, items) in tabs)
            session.EnqueuePacket(new ZC_ACK_SCHEDULER_CASHITEM
            {
                TabNum = (short)tab,
                Items = items.Select(i => new CashItemEntry(i.itemId, i.price)).ToList(),
            });
    }

    public void SendBuyResult(PlayerEntity pc, uint itemId, CashShopBuyResult result)
        => _sessions.GetByEntityId(pc.Id)?.EnqueuePacket(new ZC_PC_BUY_CASHITEM_RESULT
        { ItemId = itemId, Result = result, CashPoints = pc.CashPoints, KafraPoints = pc.KafraPoints });

    public void SendActiveSales(PlayerEntity pc, IReadOnlyList<(int itemId, int amount, int remainingSeconds)> sales)
    {
        var session = _sessions.GetByEntityId(pc.Id);
        if (session == null) return;
        foreach (var (itemId, amount, remainingSeconds) in sales)
        {
            session.EnqueuePacket(new ZC_NOTIFY_BARGAIN_SALE_SELLING { ItemId = (uint)itemId, RemainingSeconds = remainingSeconds });
            session.EnqueuePacket(new ZC_ACK_COUNT_BARGAIN_SALE_ITEM { ItemId = (uint)itemId, Amount = amount });
        }
    }
}
