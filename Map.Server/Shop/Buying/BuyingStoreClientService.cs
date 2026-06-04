using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Buying;

/// <summary>
/// Default <see cref="IBuyingStoreClientService"/>. Routes the store sign to the area (via
/// <see cref="IVisibilityService"/>) and the owner's item list / open-failure to the buyer's session.
/// </summary>
public sealed class BuyingStoreClientService : IBuyingStoreClientService
{
    private readonly IVisibilityService _visibility;
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<BuyingStoreClientService> _logger;

    public BuyingStoreClientService(IVisibilityService visibility, ISessionManagerAccessor sessions, ILogger<BuyingStoreClientService> logger)
    {
        _visibility = visibility;
        _sessions = sessions;
        _logger = logger;
    }

    public void OpenStore(PlayerEntity buyer, int zenyLimit, string title, IReadOnlyList<BuyingStoreEntry> items)
    {
        _sessions.GetByEntityId(buyer.Id)?.EnqueuePacket(new ZC_MYITEMLIST_BUYING_STORE
        {
            AccountId = (uint)buyer.AccountId,
            ZenyLimit = zenyLimit,
            Items = items,
        });
        // Store sign to everyone in view (rAthena AREA_WOS).
        _visibility.SendToArea(buyer, new ZC_BUYING_STORE_ENTRY
        {
            MakerAccountId = (uint)buyer.AccountId,
            StoreName = title ?? string.Empty,
        }, SendTarget.AreaWos);
    }

    public void CloseStore(PlayerEntity buyer)
        => _visibility.SendToArea(buyer, new ZC_DISAPPEAR_BUYING_STORE_ENTRY { MakerAccountId = (uint)buyer.AccountId }, SendTarget.AreaWos);

    public void OpenFailed(PlayerEntity buyer, BuyingStoreOpenResult result)
        => _sessions.GetByEntityId(buyer.Id)?.EnqueuePacket(new ZC_FAILED_OPEN_BUYING_STORE { Result = result });
}
