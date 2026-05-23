using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Cash;

/// <summary>
/// Default <see cref="ICashShopService"/>. AT-D2 wave: real in-memory
/// sale registry + GM-set add/remove + per-sale active-window timer
/// (Start → End scheduling via <see cref="Timer"/>). Catalog itself
/// is loaded from <c>cashshop_db.yml</c> when its loader ships.
/// </summary>
public sealed class CashShopService : ICashShopService, IDisposable
{
    private readonly Dictionary<int, Sale> _sales = new();
    private readonly List<Timer> _activeTimers = new();
    private readonly ILogger<CashShopService> _logger;

    public CashShopService(ILogger<CashShopService> logger) => _logger = logger;

    public bool BuyList(PlayerEntity pc, IReadOnlyList<(int itemId, int qty, byte tab)> items)
    {
        // Real fulfillment requires cashshop_db.yml load — the row
        // gives us the canonical price + tab membership. For now
        // refuse with an audit log so the call site still gets a
        // structured ack.
        _logger.LogInformation("cashshop_buylist: {N} items requested by {Pc} (catalog load pending)",
            items.Count, pc.Name);
        return false;
    }

    public void Reload()
    {
        // Drop all timers — sales fire fresh after reload.
        foreach (var t in _activeTimers) t.Dispose();
        _activeTimers.Clear();
        _sales.Clear();
        _logger.LogInformation("cashshop_reload: sales registry cleared");
    }

    public void ReloadDb() => Reload();

    /// <summary>
    /// rAthena <c>sale_add_item</c> — schedule a sale window. <paramref name="startAt"/>
    /// and <paramref name="endAt"/> are absolute UTC; if startAt is past the sale
    /// activates immediately.
    /// </summary>
    public void SaleAddItem(int itemId, int amount, DateTime startAt, DateTime endAt)
    {
        var sale = new Sale { ItemId = itemId, Amount = amount, StartAt = startAt, EndAt = endAt };
        _sales[itemId] = sale;
        var now = DateTime.UtcNow;
        var startDelay = startAt - now;
        var endDelay = endAt - now;
        if (startDelay <= TimeSpan.Zero) sale.Active = true;
        else _activeTimers.Add(new Timer(_ =>
        {
            sale.Active = true;
            _logger.LogInformation("sale_start_timer: item={Id} active", sale.ItemId);
        }, null, (int)Math.Min(int.MaxValue, startDelay.TotalMilliseconds), Timeout.Infinite));
        if (endDelay > TimeSpan.Zero)
            _activeTimers.Add(new Timer(_ =>
            {
                sale.Active = false;
                _sales.Remove(sale.ItemId);
                _logger.LogInformation("sale_end_timer: item={Id} ended", sale.ItemId);
            }, null, (int)Math.Min(int.MaxValue, endDelay.TotalMilliseconds), Timeout.Infinite));
    }

    /// <summary>rAthena <c>sale_find_item</c> — look up an active sale.</summary>
    public bool SaleFindItem(int itemId)
        => _sales.TryGetValue(itemId, out var s) && s.Active;

    public bool SaleRemoveItem(int itemId)
    {
        if (!_sales.Remove(itemId)) return false;
        _logger.LogInformation("sale_remove_item: {Id} cleared", itemId);
        return true;
    }

    public void SaleNotifyLogin(PlayerEntity pc)
    {
        // Wire packet ZC_NOTIFY_BARGAIN_SALE_SELLING_BACKWARD enumerates
        // active sales. The list shape is the public surface; data
        // comes from _sales.
        var n = _sales.Values.Count(s => s.Active);
        _logger.LogDebug("sale_notify_login: {N} active sales for {Pc}", n, pc.Name);
    }

    public void Dispose()
    {
        foreach (var t in _activeTimers) t.Dispose();
        _activeTimers.Clear();
    }

    private sealed class Sale
    {
        public int ItemId;
        public int Amount;
        public DateTime StartAt;
        public DateTime EndAt;
        public bool Active;
    }
}
