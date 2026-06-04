using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Vending;

/// <summary>
/// Default <see cref="IVendingClientService"/>. Builds the vending ZC packets and routes the stall
/// sign to the area (via <see cref="IVisibilityService"/>) and the open ack to the vendor's session
/// (via <see cref="ISessionManagerAccessor"/>).
/// </summary>
public sealed class VendingClientService : IVendingClientService
{
    private readonly IVisibilityService _visibility;
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<VendingClientService> _logger;

    public VendingClientService(IVisibilityService visibility, ISessionManagerAccessor sessions, ILogger<VendingClientService> logger)
    {
        _visibility = visibility;
        _sessions = sessions;
        _logger = logger;
    }

    public void OpenStall(PlayerEntity vendor, string title)
    {
        // Stall sign to everyone in view (rAthena AREA_WOS — the vendor doesn't need their own sign).
        _visibility.SendToArea(vendor, new ZC_STORE_ENTRY
        {
            MakerAccountId = (uint)vendor.AccountId,
            StoreName = title ?? string.Empty,
        }, SendTarget.AreaWos);
        OpenAck(vendor, 0);
    }

    public void CloseStall(PlayerEntity vendor)
        => _visibility.SendToArea(vendor, new ZC_DISAPPEAR_ENTRY { OwnerId = (uint)vendor.Id.Value }, SendTarget.AreaWos);

    public void OpenAck(PlayerEntity vendor, byte result)
        => _sessions.GetByEntityId(vendor.Id)?.EnqueuePacket(new ZC_ACK_OPENSTORE2 { Result = result });

    public void SendVendingList(PlayerEntity buyer, int ownerAccountId, IReadOnlyList<VendingListEntry> items)
        => _sessions.GetByEntityId(buyer.Id)?.EnqueuePacket(new ZC_PC_PURCHASE_ITEMLIST_FROMMC
        { OwnerAccountId = (uint)ownerAccountId, Items = items });

    public void SendPurchaseResult(PlayerEntity buyer, short clientIndex, short amount, VendPurchaseResult result)
        => _sessions.GetByEntityId(buyer.Id)?.EnqueuePacket(new ZC_PC_PURCHASE_RESULT_FROMMC
        { Index = clientIndex, Amount = amount, Result = result });

    public void SendVendorReport(PlayerEntity vendor, short clientIndex, short amount)
        => _sessions.GetByEntityId(vendor.Id)?.EnqueuePacket(new ZC_DELETEITEM_FROM_MCSTORE
        { Index = clientIndex, Amount = amount });

    public void SendMyItemList(PlayerEntity vendor, IReadOnlyList<VendingListEntry> items)
        => _sessions.GetByEntityId(vendor.Id)?.EnqueuePacket(new ZC_PC_PURCHASE_MYITEMLIST
        { OwnerId = (uint)vendor.Id.Value, Items = items });
}
