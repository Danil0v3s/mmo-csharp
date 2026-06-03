using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Status;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Cash;

/// <summary>
/// Default <see cref="ICashShopService"/> (FEATURE-13). Real cash-shop purchase: the
/// <c>cashshop_db</c> catalog (tab → item → cash price) is loaded from
/// <see cref="Core.Database.Repositories.Api.IItemCashDbRepository"/> on boot, and
/// <see cref="BuyList"/> resolves each item's price (applying the active sale discount on the Sale
/// tab), validates the whole basket (weight + inventory space + affordability), debits the
/// kafra-then-cash point split, grants the items, and decrements consumed sale stock — all
/// or nothing. The sale-window timer machinery is unchanged.
///
/// The CZ buy request handler + the ZC ack/result/cash-point-list/sale-list packet emits are the
/// PACKET-08 seam (cash-shop client packets); this service is the parity-faithful core they drive.
/// </summary>
public sealed class CashShopService : ICashShopService, IDisposable
{
    // rAthena e_cash_shop_tab (cashshop.hpp:27) — tab index order.
    private const int TabSale = 8;     // CASHSHOP_TAB_SALE
    private const int TabMax = 9;      // CASHSHOP_TAB_MAX
    private const int MaxBuyQty = 99;  // client blocks >99 of one item per buy
    private const int MaxInventory = 100; // rAthena MAX_INVENTORY
    private const int BaseMaxWeight = 20000; // rAthena pc.cpp base max weight
    private const int DefaultStackMax = 30000; // rAthena MAX_AMOUNT

    /// <summary>rAthena <c>e_cash_shop_tab</c> name → index. The <c>item_cash_db</c> Tab column stores
    /// the YAML enum name; map it to the canonical tab index a purchase quotes.</summary>
    private static readonly IReadOnlyDictionary<string, int> TabIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["New"] = 0, ["NewItem"] = 0,
        ["Hot"] = 1, ["Popular"] = 1,
        ["Limited"] = 2,
        ["Rental"] = 3,
        ["Permanent"] = 4, ["Perpetuity"] = 4,
        ["Scrolls"] = 5, ["Buff"] = 5,
        ["Consumables"] = 6, ["Recovery"] = 6,
        ["Other"] = 7, ["Etc"] = 7,
        ["Sale"] = TabSale, ["Special"] = TabSale,
    };

    private readonly Dictionary<int, Sale> _sales = new();
    private readonly List<Timer> _activeTimers = new();
    // _catalog[tab][nameId] = cash price.
    private readonly Dictionary<int, Dictionary<uint, int>> _catalog = new();
    private readonly ILogger<CashShopService> _logger;
    private readonly IServiceScopeFactory? _scopes;
    private readonly IItemCatalog? _items;
    private readonly ISessionManagerAccessor? _sessions;
    private readonly IInventoryService? _inventory;

    public CashShopService(
        ILogger<CashShopService> logger,
        IServiceScopeFactory? scopes = null,
        IItemCatalog? items = null,
        ISessionManagerAccessor? sessions = null,
        IInventoryService? inventory = null)
    {
        _logger = logger;
        _scopes = scopes;
        _items = items;
        _sessions = sessions;
        _inventory = inventory;
        Reload();
    }

    /// <summary>
    /// FEATURE-13 — rAthena <c>cashshop_buylist</c> (cashshop.cpp:441). Validates the whole basket
    /// first (tab, catalog membership, qty, sale stock, inventory space, weight), then pays the
    /// kafra-then-cash split and grants every item. No mutation occurs on any rejection.
    /// </summary>
    public CashShopResult BuyList(PlayerEntity pc, int kafraPay, IReadOnlyList<(int itemId, int qty, byte tab)> items)
    {
        if (items.Count == 0) return CashShopResult.System;

        var session = _sessions?.GetByEntityId(pc.Id);
        if (session?.Inventory == null) return CashShopResult.System;
        var inv = session.Inventory;

        long totalCash = 0;
        long totalWeight = 0;
        var freshSlots = 0;
        var plan = new List<(uint nameId, int qty, bool fromSale)>(items.Count);
        // Track per-nameid running totals so a basket buying the same item twice validates correctly.
        var pendingAmount = new Dictionary<uint, int>();
        var pendingSlots = new Dictionary<uint, int>();

        foreach (var (rawId, qty, tab) in items)
        {
            if (qty <= 0 || qty > MaxBuyQty) return CashShopResult.Amount;
            if (tab >= TabMax) return CashShopResult.System;

            var nameId = (uint)rawId;
            // rAthena findItemInTab(tab, nameid): the price is the entry under THIS tab — the Sale
            // tab carries its own (discounted) price, so buying via Sale charges the lower amount.
            var unitPrice = FindItemInTab(tab, nameId);
            if (unitPrice < 0) return CashShopResult.PurchaseFail; // not in the catalog for this tab

            var def = _items?.Get(nameId);
            if (def == null) return CashShopResult.ItemId;

            var fromSale = false;
            if (tab == TabSale)
            {
                if (!_sales.TryGetValue((int)nameId, out var sale) || !sale.Active)
                    return CashShopResult.PurchaseFail; // not actually on sale
                if (sale.Amount < qty)
                    return CashShopResult.Amount;        // not enough sale stock
                fromSale = true;
            }

            // rAthena pc_checkadditem — EXIST (merge), NEW (+slots), OVERAMOUNT (over stack max).
            var type = ItemTypeCodes.FromDbString(def.Type);
            var stackable = !ItemTypeCodes.IsEquippable(type) && (def.FlagUniqueid ?? 0) == 0;
            var stackMax = def.StackAmount.HasValue && def.StackAmount.Value > 0 ? def.StackAmount.Value : DefaultStackMax;

            if (stackable)
            {
                var existing = inv.Where(it => it.NameId == nameId).Sum(it => (long)it.Amount);
                var already = pendingAmount.GetValueOrDefault(nameId);
                if (existing + already + qty > stackMax) return CashShopResult.Amount;
                if (existing == 0 && already == 0) { freshSlots += 1; pendingSlots[nameId] = 1; }
                pendingAmount[nameId] = already + qty;
            }
            else
            {
                freshSlots += qty; // one slot per non-stackable instance
            }

            totalCash += (long)unitPrice * qty;
            totalWeight += (long)(def.Weight ?? 0) * qty;
            plan.Add((nameId, qty, fromSale));
        }

        // rAthena: (totalweight + sd->weight) > sd->max_weight.
        if (CurrentWeight(inv) + totalWeight > MaxWeight(pc)) return CashShopResult.InventoryWeight;
        // rAthena: pc_inventoryblank(sd) < new_.
        if (MaxInventory - inv.Count < freshSlots) return CashShopResult.InventoryWeight;

        // rAthena pc_paycash: kafra first (capped to kafraPay + balance), remainder from cash points.
        if (totalCash > int.MaxValue) return CashShopResult.Money;
        if (!TryPayCash(pc, (int)totalCash, kafraPay)) return CashShopResult.Money;

        // All gates passed — grant + decrement sale stock.
        foreach (var (nameId, qty, fromSale) in plan)
        {
            var def = _items!.Get(nameId)!;
            var type = ItemTypeCodes.FromDbString(def.Type);
            var stackable = !ItemTypeCodes.IsEquippable(type) && (def.FlagUniqueid ?? 0) == 0;
            var getAmt = stackable ? qty : 1;
            for (var j = 0; j < qty; j += getAmt)
                _inventory?.GiveItem(session, nameId, getAmt);

            if (fromSale && _sales.TryGetValue((int)nameId, out var sale))
            {
                sale.Amount -= qty;
                if (sale.Amount <= 0) SaleRemoveItem((int)nameId);
            }
        }

        _logger.LogInformation("cashshop_buylist: {Pc} bought {N} line(s) for {Cash} points",
            pc.Name, plan.Count, totalCash);
        return CashShopResult.Success;
    }

    public void Reload()
    {
        foreach (var t in _activeTimers) t.Dispose();
        _activeTimers.Clear();
        _sales.Clear();
        LoadCatalog();
        _logger.LogInformation("cashshop_reload: sales cleared, catalog has {N} item(s) across {T} tab(s)",
            _catalog.Values.Sum(t => t.Count), _catalog.Count);
    }

    public void ReloadDb() => Reload();

    /// <summary>
    /// rAthena <c>sale_add_item</c> — schedule a sale window. <paramref name="startAt"/>
    /// and <paramref name="endAt"/> are absolute UTC; if startAt is past the sale
    /// activates immediately. The sale price is the item's entry in the Sale tab of the catalog
    /// (rAthena populates the Sale tab when an item goes on sale).
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

    /// <summary>rAthena <c>sale_notify_login</c> — surface the active-sale list to a logging-in
    /// player. The data half (the active sale snapshot) is real; the ZC_NOTIFY_BARGAIN_SALE list
    /// packet emit is the PACKET-08 seam.</summary>
    public void SaleNotifyLogin(PlayerEntity pc)
    {
        var active = ActiveSales();
        // PACKET-08: emit ZC_ACK_SCHEDULER_CASHITEM / clif_sale_amount for each entry to `pc`.
        _logger.LogDebug("sale_notify_login: {N} active sale(s) for {Pc}", active.Count, pc.Name);
    }

    /// <summary>FEATURE-13 — the currently-active sale snapshot (item id + discounted price +
    /// remaining stock). Real data the PACKET-08 sale-list emit + tests consume.</summary>
    public IReadOnlyList<(int itemId, int price, int amount)> ActiveSales()
    {
        var list = new List<(int, int, int)>();
        foreach (var s in _sales.Values)
            if (s.Active)
                list.Add((s.ItemId, SalePrice(s.ItemId), s.Amount));
        return list;
    }

    public void Dispose()
    {
        foreach (var t in _activeTimers) t.Dispose();
        _activeTimers.Clear();
    }

    // --- catalog ---

    private void LoadCatalog()
    {
        _catalog.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<Core.Database.Repositories.Api.IItemCashDbRepository>();
            foreach (var tab in repo.GetAllAsync().GetAwaiter().GetResult())
            {
                if (!TabIndex.TryGetValue(tab.Tab, out var tabIdx)) continue;
                var dict = _catalog.TryGetValue(tabIdx, out var d) ? d : (_catalog[tabIdx] = new Dictionary<uint, int>());
                foreach (var entry in repo.GetEntriesAsync(tab.Tab).GetAwaiter().GetResult())
                {
                    var nameId = ResolveAegis(entry.ItemAegis);
                    if (nameId == 0) continue;
                    dict[nameId] = entry.Price;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "cashshop catalog load failed");
        }
    }

    private uint ResolveAegis(string aegis)
        => _items?.GetByAegisName(aegis)?.Id ?? 0;

    /// <summary>rAthena <c>CashShopDatabase::findItemInTab</c> — the cash price for
    /// <paramref name="nameId"/> listed under <paramref name="tab"/>, or -1 if it isn't in that tab.</summary>
    private int FindItemInTab(int tab, uint nameId)
        => _catalog.TryGetValue(tab, out var d) && d.TryGetValue(nameId, out var p) ? p : -1;

    /// <summary>The Sale-tab catalog price for a sale entry (0 if the item isn't catalogued there).</summary>
    private int SalePrice(int nameId)
    {
        var p = FindItemInTab(TabSale, (uint)nameId);
        return p < 0 ? 0 : p;
    }

    // --- weight / pay (mirrors PlayerWeightStatusService + pc_paycash) ---

    private long CurrentWeight(IReadOnlyList<InventoryItem> inv)
    {
        long w = 0;
        if (_items == null) return 0;
        foreach (var row in inv)
        {
            if (row.Amount == 0) continue;
            w += (long)(_items.Get(row.NameId)?.Weight ?? 0) * row.Amount;
        }
        return w;
    }

    private static long MaxWeight(PlayerEntity pc) => BaseMaxWeight + Math.Max(0, pc.EquipBonuses.AddMaxWeight);

    /// <summary>rAthena <c>pc_paycash</c> (pc.cpp:5475) — kafra points first (capped to
    /// <paramref name="kafraPay"/> and the balance), remainder from cash points. Refuses on an
    /// insufficient total; no debit on refusal.</summary>
    private bool TryPayCash(PlayerEntity pc, int price, int kafraPay)
    {
        if (price < 0) return false;
        var kafra = Math.Min(Math.Max(0, kafraPay), pc.KafraPoints);
        kafra = Math.Min(kafra, price);
        var cashOwed = price - kafra;
        if (cashOwed > pc.CashPoints) return false;
        pc.KafraPoints -= kafra;
        pc.CashPoints -= cashOwed;
        _logger.LogInformation("pc_paycash: char {Char} paid kafra={Kafra} cash={Cash} (remaining K={K} C={C})",
            pc.CharacterId, kafra, cashOwed, pc.KafraPoints, pc.CashPoints);
        return true;
    }

    // --- test seam ---

    /// <summary>FEATURE-13 test seam — seed catalog rows without a DB round-trip.</summary>
    internal void SeedCatalogForTest(int tab, params (uint nameId, int price)[] entries)
    {
        var dict = _catalog.TryGetValue(tab, out var d) ? d : (_catalog[tab] = new Dictionary<uint, int>());
        foreach (var (nameId, price) in entries) dict[nameId] = price;
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
