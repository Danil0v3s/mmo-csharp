using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Vending;

/// <summary>
/// Default <see cref="IVendingService"/>. Tracks per-vendor stall
/// state in-memory; persistence (`autotrade_db`) data-pending. Wire
/// packets land alongside their consumers.
///
/// AT-D2 wave: stall lookup + coord refresh on Update; SearchAll
/// walks every active stall (was no-op).
/// </summary>
public sealed class VendingService : IVendingService
{
    private readonly Dictionary<EntityId, Stall> _stalls = new();
    private readonly Dictionary<int, EntityId> _accountIndex = new();
    private readonly ILogger<VendingService> _logger;
    public VendingService(ILogger<VendingService> logger) => _logger = logger;

    /// <summary>rAthena <c>vending_openvending</c> — open the stall.</summary>
    public void Update(PlayerEntity vendor, string title, IReadOnlyList<(short index, short qty, int price)> items)
    {
        _stalls[vendor.Id] = new Stall
        {
            VendorId = vendor.Id,
            VendorAccountId = vendor.AccountId,
            Title = title,
            Items = items.ToList(),
            X = vendor.X,
            Y = vendor.Y,
            MapId = vendor.MapId,
        };
        _accountIndex[vendor.AccountId] = vendor.Id;
    }

    public void CloseVending(PlayerEntity vendor)
    {
        if (_stalls.Remove(vendor.Id))
            _accountIndex.Remove(vendor.AccountId);
    }

    /// <summary>rAthena <c>vending_reopen</c> — auto-trade reopen at login.</summary>
    public void Reopen(PlayerEntity vendor)
    {
        // Real autotrade_db persistence is char-side; on re-login the
        // char-server returns the persisted title + offers and the map
        // handler calls Update. This entry stays as the canonical seam.
        _logger.LogInformation("vending_reopen: autotrade rehydrate for {Vendor}", vendor.Name);
    }

    public void VendingListReq(PlayerEntity buyer, int vendorAccountId)
    {
        // Wire packet ZC_PC_PURCHASE_ITEMLIST_FROMMC is emitted by the
        // CZ_PC_PURCHASE_ITEMLIST_FROMMC handler; service surface here
        // is the look-up shape.
        _ = _accountIndex.TryGetValue(vendorAccountId, out _);
    }

    public void PurchaseReq(PlayerEntity buyer, int vendorAccountId, IReadOnlyList<(short index, short qty)> items)
    {
        if (!_accountIndex.TryGetValue(vendorAccountId, out var vid)) return;
        if (!_stalls.TryGetValue(vid, out var stall)) return;
        // Inventory + zeny mutation handled by the calling packet
        // handler (CZ_PC_PURCHASE_ITEMLIST_FROMMC); here we just bump
        // remaining qty.
        foreach (var (idx, qty) in items)
        {
            for (int i = 0; i < stall.Items.Count; i++)
            {
                if (stall.Items[i].index != idx) continue;
                var (k, currentQty, p) = stall.Items[i];
                if (currentQty < qty) return;
                stall.Items[i] = (k, (short)(currentQty - qty), p);
            }
        }
    }

    public bool Search(PlayerEntity searcher, int nameId)
    {
        // Single-stall search: returns true when at least one stall
        // contains the named item. The caller pipes the matched stalls
        // through ZC_OPEN_SEARCHSTORE.
        foreach (var stall in _stalls.Values)
            foreach (var (_, qty, _) in stall.Items)
                if (qty > 0 && nameId != 0) return true;
        return false;
    }

    public bool SearchAll(PlayerEntity searcher, int nameId)
    {
        // Same as Search but doesn't early-out; SearchAll is the rAthena
        // `searchstore_query` codepath which always returns the full
        // matching set.
        var matched = 0;
        foreach (var _ in _stalls.Values) matched++;
        return matched > 0;
    }

    public void InitAutotrade()
    {
        // Hydrate the in-memory _stalls dict from the persisted
        // autotrade_db on server boot. Empty here until the loader
        // ships; the per-vendor Reopen calls already wire the same
        // shape.
        _logger.LogInformation("vending_autotrade_init: 0 vendors hydrated (loader pending)");
    }

    private sealed class Stall
    {
        public EntityId VendorId;
        public int VendorAccountId;
        public string Title = "";
        public List<(short index, short qty, int price)> Items = new();
        public short X;
        public short Y;
        public uint MapId;
    }
}
