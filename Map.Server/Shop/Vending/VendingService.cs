using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Vending;

/// <summary>
/// Default <see cref="IVendingService"/>. Tracks per-vendor stall state in-memory and performs the
/// real buyer↔vendor transfer on purchase (FEATURE-11): zeny (minus the vending tax) + the item from
/// the vendor's <b>cart</b> to the buyer's inventory, all-or-nothing. Each open stamps a
/// <c>VenderId</c> so a stale client packet can't buy at old prices. Autotrade persistence
/// (offline-vendor row + NPC respawn) is FEATURE-35.
/// </summary>
public sealed class VendingService : IVendingService
{
    private const int MaxInventory = 100; // rAthena MAX_INVENTORY

    private readonly Dictionary<EntityId, Stall> _stalls = new();
    private readonly Dictionary<int, EntityId> _accountIndex = new();
    private readonly ISessionManagerAccessor? _sessions;
    private readonly ILogger<VendingService> _logger;
    private long _nextVenderId = 1;

    /// <summary>rAthena <c>battle_config.vending_tax</c> in basis points (1/10000). Default 0; the
    /// vendor's zeny gain is reduced by it. Internal-settable for tests.</summary>
    internal long VendingTaxBp { get; set; } = 0;

    public VendingService(ILogger<VendingService> logger, ISessionManagerAccessor? sessions = null)
    {
        _logger = logger;
        _sessions = sessions;
    }

    /// <summary>rAthena <c>vending_openvending</c> — open (or refresh) the stall, stamping a fresh
    /// vender id.</summary>
    public void Update(PlayerEntity vendor, string title, IReadOnlyList<(short index, short qty, int price)> items)
    {
        _stalls[vendor.Id] = new Stall
        {
            VendorId = vendor.Id,
            VendorAccountId = vendor.AccountId,
            VenderId = _nextVenderId++,
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

    /// <summary>rAthena <c>vending_reopen</c> — auto-trade reopen at login. ➡️ The char-side persisted
    /// stall hydrate is FEATURE-35; this stays the wire seam the response calls <see cref="Update"/> on.</summary>
    public void Reopen(PlayerEntity vendor)
        => _logger.LogInformation("vending_reopen: autotrade rehydrate for {Vendor} (persistence → FEATURE-35)", vendor.Name);

    public void VendingListReq(PlayerEntity buyer, int vendorAccountId)
        => _ = _accountIndex.TryGetValue(vendorAccountId, out _);

    /// <summary>
    /// FEATURE-11 — rAthena <c>vending_purchasereq</c>: the real trade. Validates the vender id
    /// (anti-desync) + every requested item (listed qty, vendor cart stock, buyer zeny, buyer inventory
    /// space) BEFORE any mutation, then transfers zeny (buyer pays full, vendor receives minus tax) and
    /// the items (vendor cart → buyer inventory). Decrements the stall and auto-closes when sold out.
    /// Returns false (no partial transfer) on any gate failure.
    /// </summary>
    public bool PurchaseReq(PlayerEntity buyer, int vendorAccountId, long venderId,
        IReadOnlyList<(short index, short qty)> items)
    {
        if (!_accountIndex.TryGetValue(vendorAccountId, out var vid)) return false;
        if (!_stalls.TryGetValue(vid, out var stall)) return false;
        if (stall.VenderId != venderId) return false; // stale stall id — anti-desync

        var buyerSession = _sessions?.GetByEntityId(buyer.Id);
        var vendorSession = _sessions?.GetByAccountId(vendorAccountId);
        var buyerInv = buyerSession?.Inventory;
        var vendorCart = vendorSession?.Cart;
        if (buyerSession?.CharacterData == null || buyerInv == null) return false;
        if (vendorSession?.CharacterData == null || vendorCart == null) return false;

        // Pass 1 — validate the full request + build the transfer plan.
        var plan = new List<(int stallSlot, InventoryItem cartItem, int qty, int price)>();
        long grandTotal = 0;
        foreach (var (idx, qty) in items)
        {
            if (qty <= 0) return false;
            var slot = stall.Items.FindIndex(it => it.index == idx);
            if (slot < 0) return false;
            var (_, listedQty, price) = stall.Items[slot];
            if (listedQty < qty || price < 0) return false;
            var cartItem = FindBySlot(vendorCart, idx);
            if (cartItem == null || cartItem.Amount < (uint)qty) return false; // vendor no longer holds it
            grandTotal += (long)price * qty;
            plan.Add((slot, cartItem, qty, price));
        }
        if (grandTotal < 0 || (long)buyerSession.CharacterData.Zeny < grandTotal) return false; // can't afford

        var freshSlots = plan.Count(p => FindMergeable(buyerInv, p.cartItem) == null);
        if (buyerInv.Count + freshSlots > MaxInventory) return false; // buyer inventory full

        // Pass 2 — transfer (all-or-nothing).
        var tax = grandTotal * VendingTaxBp / 10000;
        buyerSession.CharacterData.Zeny = (uint)((long)buyerSession.CharacterData.Zeny - grandTotal);
        vendorSession.CharacterData.Zeny = (uint)((long)vendorSession.CharacterData.Zeny + (grandTotal - tax));

        foreach (var (slot, cartItem, qty, _) in plan)
        {
            CreditBuyer(buyerInv, cartItem, qty);
            DebitCart(vendorSession, vendorCart, cartItem, qty);
            var (k, listedQty, price) = stall.Items[slot];
            stall.Items[slot] = (k, (short)(listedQty - qty), price);
        }

        // Auto-close when every listed item is sold out (rAthena vending_closevending).
        if (stall.Items.All(it => it.qty <= 0))
        {
            _stalls.Remove(vid);
            _accountIndex.Remove(vendorAccountId);
        }
        // PACKET-* (CZ/ZC vending): ZC_PC_PURCHASE_RESULT_FROMMC (buyer) + vendor sale notice emit seam.
        _logger.LogInformation("vending_purchasereq: {Buyer} bought {N} item(s) for {Total}z (tax {Tax}) from acc {Vendor}",
            buyer.Name, plan.Count, grandTotal, tax, vendorAccountId);
        return true;
    }

    public bool Search(PlayerEntity searcher, int nameId)
    {
        foreach (var stall in _stalls.Values)
            foreach (var (_, qty, _) in stall.Items)
                if (qty > 0 && nameId != 0) return true;
        return false;
    }

    public bool SearchAll(PlayerEntity searcher, int nameId)
        => _stalls.Count > 0;

    /// <summary>rAthena <c>vending_autotrade_init</c> — boot hydrate of offline autotrade vendors.
    /// ➡️ The persisted-vendor loader (EF entity + repo + NPC respawn) is FEATURE-35.</summary>
    public void InitAutotrade()
        => _logger.LogInformation("vending_autotrade_init: 0 offline vendors hydrated (persistence → FEATURE-35)");

    /// <summary>FEATURE-11 test seam — the current vender id of a stall (the value the buyer's purchase
    /// packet must echo back).</summary>
    internal long? VenderIdOf(EntityId vendorId) => _stalls.TryGetValue(vendorId, out var s) ? s.VenderId : null;

    // --- inventory/cart helpers (validate-all-then-mutate, full fidelity) ---

    private static InventoryItem? FindBySlot(List<InventoryItem> inv, int serverIndex)
    {
        foreach (var i in inv) if (i.ServerIndex == serverIndex) return i;
        return null;
    }

    private static InventoryItem? FindMergeable(List<InventoryItem> inv, InventoryItem src)
    {
        foreach (var i in inv)
        {
            if (i.NameId != src.NameId || i.Refine != src.Refine || i.Identified != src.Identified) continue;
            if (i.Card0 != src.Card0 || i.Card1 != src.Card1 || i.Card2 != src.Card2 || i.Card3 != src.Card3) continue;
            return i;
        }
        return null;
    }

    private static void CreditBuyer(List<InventoryItem> inv, InventoryItem src, int qty)
    {
        var mergeable = FindMergeable(inv, src);
        if (mergeable != null) { mergeable.Amount += (uint)qty; return; }
        var nextSlot = 0;
        foreach (var i in inv) if (i.ServerIndex >= nextSlot) nextSlot = i.ServerIndex + 1;
        inv.Add(new InventoryItem
        {
            ServerIndex = nextSlot, NameId = src.NameId, Amount = (uint)qty,
            Identified = src.Identified, Refine = src.Refine, Attribute = src.Attribute,
            Card0 = src.Card0, Card1 = src.Card1, Card2 = src.Card2, Card3 = src.Card3,
            Options = (ItemOption[])src.Options.Clone(), ExpireTime = src.ExpireTime,
            Bound = src.Bound, UniqueId = src.UniqueId, EnchantGrade = src.EnchantGrade,
        });
    }

    private static void DebitCart(Map.Server.MapSessionData session, List<InventoryItem> cart, InventoryItem cartItem, int qty)
    {
        cartItem.Amount -= (uint)qty;
        if (cartItem.Amount == 0)
        {
            if (cartItem.Id > 0) session.RemovedInventoryIds.Add(cartItem.Id);
            cart.Remove(cartItem);
        }
    }

    private sealed class Stall
    {
        public EntityId VendorId;
        public int VendorAccountId;
        public long VenderId;
        public string Title = "";
        public List<(short index, short qty, int price)> Items = new();
        public short X;
        public short Y;
        public uint MapId;
    }
}
