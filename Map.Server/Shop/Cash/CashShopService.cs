using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Cash;

/// <summary>
/// Default <see cref="ICashShopService"/>. The catalog is loaded
/// from <c>cashshop_db.yml</c> when its loader ships; for now every
/// buy call refuses with "data-pending".
/// </summary>
public sealed class CashShopService : ICashShopService
{
    private readonly ILogger<CashShopService> _logger;
    public CashShopService(ILogger<CashShopService> logger) => _logger = logger;

    public bool BuyList(PlayerEntity pc, IReadOnlyList<(int itemId, int qty, byte tab)> items) => false;
    public void Reload() { }
    public void ReloadDb() { }
    public bool SaleRemoveItem(int itemId) => false;
    public void SaleNotifyLogin(PlayerEntity pc) { }
}
