using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;
using Microsoft.Extensions.Logging;
using R = Core.Server.Packets.Out.ZC.VendPurchaseResult;

namespace Map.Server.Shop.Vending;

/// <summary>
/// Default <see cref="IVendingService"/>. Tracks per-vendor stall state in-memory and performs the
/// real buyer↔vendor transfer on purchase (FEATURE-11): zeny (minus the vending tax) + the item from
/// the vendor's <b>cart</b> to the buyer's inventory, all-or-nothing. Each open stamps a
/// <c>VenderId</c> so a stale client packet can't buy at old prices. Autotrade persistence
/// (offline-vendor row + NPC respawn) is GP-AUTOTRADE-RUNTIME.
/// </summary>
public sealed class VendingService : IVendingService
{
    private const int MaxInventory = 100; // rAthena MAX_INVENTORY

    private readonly Dictionary<EntityId, Stall> _stalls = new();
    private readonly Dictionary<int, EntityId> _accountIndex = new();
    private readonly ISessionManagerAccessor? _sessions;
    private readonly IVendingClientService? _client;
    private readonly Map.Server.Items.IItemCatalog? _items;
    private readonly IEntityRegistry? _entities;
    private readonly ILogger<VendingService> _logger;
    private long _nextVenderId = 1;

    /// <summary>rAthena <c>battle_config.vending_tax</c> in basis points (1/10000). Default 0; the
    /// vendor's zeny gain is reduced by it. Internal-settable for tests.</summary>
    internal long VendingTaxBp { get; set; } = 0;

    public VendingService(ILogger<VendingService> logger, ISessionManagerAccessor? sessions = null,
        IVendingClientService? client = null, Map.Server.Items.IItemCatalog? items = null,
        IEntityRegistry? entities = null)
    {
        _logger = logger;
        _sessions = sessions;
        _client = client;
        _items = items;
        _entities = entities;
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
        // rAthena vending_openvending → clif_openvending (the vendor's own item list + ack) +
        // clif_showvendingboard (the stall sign on-map).
        var vendorCart = _sessions?.GetByEntityId(vendor.Id)?.Cart;
        if (vendorCart != null)
            _client?.SendMyItemList(vendor, BuildListEntries(_stalls[vendor.Id], vendorCart));
        _client?.OpenStall(vendor, title);
    }

    /// <summary>Build the per-item price-list rows for a stall from its offers + the vendor's cart
    /// (resolving each item's type/identify/refine/cards). Shared by the vendor's own-list (open) and
    /// the buyer's vending list (browse).</summary>
    private List<Core.Server.Packets.Out.ZC.VendingListEntry> BuildListEntries(Stall stall, List<InventoryItem> vendorCart)
    {
        var entries = new List<Core.Server.Packets.Out.ZC.VendingListEntry>();
        foreach (var (idx, qty, price) in stall.Items)
        {
            if (qty <= 0) continue;
            var cartItem = FindBySlot(vendorCart, idx);
            if (cartItem == null) continue;
            entries.Add(new Core.Server.Packets.Out.ZC.VendingListEntry
            {
                Price = price,
                Amount = qty,
                Index = (short)(idx + 2), // client index
                ItemType = _items != null ? Map.Server.Inventory.ItemTypeCodes.FromDbString(_items.Get(cartItem.NameId)?.Type) : (byte)0,
                NameId = (short)cartItem.NameId,
                Identified = (byte)(cartItem.Identified ? 1 : 0),
                Damaged = (byte)cartItem.Attribute,
                Refine = cartItem.Refine,
                Card0 = (short)cartItem.Card0,
                Card1 = (short)cartItem.Card1,
                Card2 = (short)cartItem.Card2,
                Card3 = (short)cartItem.Card3,
            });
        }
        return entries;
    }

    public void CloseVending(PlayerEntity vendor)
    {
        if (_stalls.Remove(vendor.Id))
        {
            _accountIndex.Remove(vendor.AccountId);
            _client?.CloseStall(vendor); // rAthena clif_closevendingboard
        }
    }

    /// <summary>rAthena <c>vending_reopen</c> — auto-trade reopen at login. ➡️ The char-side persisted
    /// stall hydrate is GP-AUTOTRADE-RUNTIME; this stays the wire seam the response calls <see cref="Update"/> on.</summary>
    public void Reopen(PlayerEntity vendor)
        => _logger.LogInformation("vending_reopen: autotrade rehydrate for {Vendor} (persistence → GP-AUTOTRADE-RUNTIME)", vendor.Name);

    /// <summary>rAthena <c>vending_vendinglistreq</c> — the buyer clicked a stall: stamp the viewed
    /// vender id on the buyer (anti-desync) and send the shop's price list (<c>clif_vendinglist</c>).</summary>
    public void VendingListReq(PlayerEntity buyer, int vendorAccountId)
    {
        if (!_accountIndex.TryGetValue(vendorAccountId, out var vid)) return;
        if (!_stalls.TryGetValue(vid, out var stall)) return;
        var vendorCart = _sessions?.GetByAccountId(vendorAccountId)?.Cart;
        if (vendorCart == null) return;

        buyer.VendedId = stall.VenderId; // rAthena sd->vended_id
        _client?.SendVendingList(buyer, vendorAccountId, BuildListEntries(stall, vendorCart));
    }

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
        if (stall.VenderId != venderId) { Fail(buyer, 0, 0, R.StoreIncorrect); return false; } // anti-desync

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
            var clientIdx = (short)(idx + 2);
            if (qty <= 0) return false;
            var slot = stall.Items.FindIndex(it => it.index == idx);
            if (slot < 0) { Fail(buyer, clientIdx, qty, R.OutOfStock); return false; } // picked a non-listed item
            var (_, listedQty, price) = stall.Items[slot];
            if (price < 0) return false;
            var cartItem = FindBySlot(vendorCart, idx);
            if (listedQty < qty || cartItem == null || cartItem.Amount < (uint)qty)
            { Fail(buyer, clientIdx, listedQty > 0 ? listedQty : (short)0, R.OutOfStock); return false; } // not enough stock
            grandTotal += (long)price * qty;
            if (grandTotal < 0 || (long)buyerSession.CharacterData.Zeny < grandTotal)
            { Fail(buyer, clientIdx, qty, R.NoZeny); return false; } // can't afford
            plan.Add((slot, cartItem, qty, price));
        }

        var freshSlots = plan.Count(p => FindMergeable(buyerInv, p.cartItem) == null);
        if (buyerInv.Count + freshSlots > MaxInventory) return false; // buyer inventory full (rAthena: silent)

        // Pass 2 — transfer (all-or-nothing).
        var tax = grandTotal * VendingTaxBp / 10000;
        buyerSession.CharacterData.Zeny = (uint)((long)buyerSession.CharacterData.Zeny - grandTotal);
        vendorSession.CharacterData.Zeny = (uint)((long)vendorSession.CharacterData.Zeny + (grandTotal - tax));

        var vendorEntity = _entities?.Get(stall.VendorId) as PlayerEntity;
        foreach (var (slot, cartItem, qty, _) in plan)
        {
            var nameId = cartItem.NameId;
            var bought = CreditBuyer(buyerInv, cartItem, qty);
            DebitCart(vendorSession, vendorCart, cartItem, qty);
            var (k, listedQty, price) = stall.Items[slot];
            stall.Items[slot] = (k, (short)(listedQty - qty), price);

            // Buyer sees the new item; the vendor gets the sale notice (rAthena clif_vendingreport).
            EmitPickup(buyerSession, bought, nameId, qty);
            if (vendorEntity != null) _client?.SendVendorReport(vendorEntity, (short)(k + 2), (short)qty);
        }

        // Zeny updates (rAthena pc_payzeny / pc_getzeny send the SP_ZENY par-change).
        EmitZeny(buyerSession, (int)buyerSession.CharacterData.Zeny);
        EmitZeny(vendorSession, (int)vendorSession.CharacterData.Zeny);

        // Auto-close when every listed item is sold out (rAthena vending_closevending).
        if (stall.Items.All(it => it.qty <= 0))
        {
            _stalls.Remove(vid);
            _accountIndex.Remove(vendorAccountId);
            if (vendorEntity != null) _client?.CloseStall(vendorEntity);
        }
        _logger.LogInformation("vending_purchasereq: {Buyer} bought {N} item(s) for {Total}z (tax {Tax}) from acc {Vendor}",
            buyer.Name, plan.Count, grandTotal, tax, vendorAccountId);
        return true;
    }

    private void Fail(PlayerEntity buyer, short clientIndex, short amount, R result)
        => _client?.SendPurchaseResult(buyer, clientIndex, amount, result);

    private static void EmitZeny(Map.Server.MapSessionData session, int zeny)
        => session.EnqueuePacket(new Core.Server.Packets.Out.ZC.ZC_PAR_CHANGE
        { VarId = Core.Server.Packets.SpId.SP_ZENY, Value = zeny });

    private void EmitPickup(Map.Server.MapSessionData buyerSession, InventoryItem bought, uint nameId, int qty)
        => buyerSession.EnqueuePacket(new Core.Server.Packets.Out.ZC.ZC_ITEM_PICKUP_ACK
        {
            Index = (short)(bought.ServerIndex + 2),
            Count = (short)qty,
            NameId = nameId,
            IsIdentified = (byte)(bought.Identified ? 1 : 0),
            IsDamaged = (byte)bought.Attribute,
            Card0 = bought.Card0, Card1 = bought.Card1, Card2 = bought.Card2, Card3 = bought.Card3,
            Type = _items != null ? Map.Server.Inventory.ItemTypeCodes.FromDbString(_items.Get(nameId)?.Type) : (byte)0,
            RefiningLevel = bought.Refine,
            Result = 0,
        });

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
    /// ➡️ The persisted-vendor loader (EF entity + repo + NPC respawn) is GP-AUTOTRADE-RUNTIME.</summary>
    public void InitAutotrade()
        => _logger.LogInformation("vending_autotrade_init: 0 offline vendors hydrated (persistence → GP-AUTOTRADE-RUNTIME)");

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

    /// <summary>Add the bought item to the buyer (merge if stackable, else a fresh slot). Returns the
    /// slot it landed in so the caller can emit the pickup ack against the right index.</summary>
    private static InventoryItem CreditBuyer(List<InventoryItem> inv, InventoryItem src, int qty)
    {
        var mergeable = FindMergeable(inv, src);
        if (mergeable != null) { mergeable.Amount += (uint)qty; return mergeable; }
        var nextSlot = 0;
        foreach (var i in inv) if (i.ServerIndex >= nextSlot) nextSlot = i.ServerIndex + 1;
        var slot = new InventoryItem
        {
            ServerIndex = nextSlot, NameId = src.NameId, Amount = (uint)qty,
            Identified = src.Identified, Refine = src.Refine, Attribute = src.Attribute,
            Card0 = src.Card0, Card1 = src.Card1, Card2 = src.Card2, Card3 = src.Card3,
            Options = (ItemOption[])src.Options.Clone(), ExpireTime = src.ExpireTime,
            Bound = src.Bound, UniqueId = src.UniqueId, EnchantGrade = src.EnchantGrade,
        };
        inv.Add(slot);
        return slot;
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
