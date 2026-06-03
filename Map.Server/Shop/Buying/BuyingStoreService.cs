using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Buying;

/// <summary>
/// Default <see cref="IBuyingStoreService"/>. A buying store is a player who pays others for items
/// they want: the buyer **escrows their zeny up to the limit on open**, a seller sells items in
/// (item → buyer, zeny paid from the escrow → seller), and the unspent escrow is **refunded on close**
/// (FEATURE-12). Autotrade persistence (offline-buyer row + NPC) is FEATURE-36.
/// </summary>
public sealed class BuyingStoreService : IBuyingStoreService
{
    private const int MaxOffers = 5;       // rAthena MAX_BUYINGSTORE_SLOTS
    private const long MaxZenyLimit = 99_999_999L;
    private const int MaxInventory = 100;  // rAthena MAX_INVENTORY

    private readonly Dictionary<EntityId, BuyStall> _stalls = new();
    private readonly Dictionary<int, EntityId> _accountIndex = new();
    private readonly ISessionManagerAccessor? _sessions;
    private readonly ILogger<BuyingStoreService> _logger;
    private uint _nextStoreId = 1;

    public BuyingStoreService(ILogger<BuyingStoreService> logger, ISessionManagerAccessor? sessions = null)
    {
        _logger = logger;
        _sessions = sessions;
    }

    /// <summary>rAthena <c>buyingstore_setup</c> — gate + seed the stall slot (no escrow yet; that
    /// happens on <see cref="Update"/>, the create).</summary>
    public void Open(PlayerEntity buyer, byte effectId)
    {
        if (effectId > 3) { _logger.LogWarning("buyingstore_setup: rejected effect={Eff} for {Buyer}", effectId, buyer.Name); return; }
        if (_stalls.ContainsKey(buyer.Id)) { _logger.LogWarning("buyingstore_setup: already open for {Buyer}", buyer.Name); return; }
        _stalls[buyer.Id] = new BuyStall
        {
            BuyerId = buyer.Id,
            BuyerAccountId = buyer.AccountId,
            StoreId = _nextStoreId++,
            EffectId = effectId,
            X = buyer.X, Y = buyer.Y, MapId = buyer.MapId,
        };
        _accountIndex[buyer.AccountId] = buyer.Id;
    }

    /// <summary>FEATURE-12 — rAthena <c>buyingstore_create</c>: set the offers + **escrow the buyer's
    /// zeny up to <paramref name="zenyLimit"/>** (held in the stall). Rejects (and tears down the
    /// stall) when the buyer can't back the limit. Returns true when the store is open + escrowed.</summary>
    public bool Update(PlayerEntity buyer, string title, long zenyLimit,
        IReadOnlyList<(int nameId, short amount, int price)> offers)
    {
        if (offers.Count > MaxOffers || zenyLimit < 0 || zenyLimit > MaxZenyLimit) return false;
        var s = _stalls.GetValueOrDefault(buyer.Id);
        if (s == null) { Open(buyer, 0); s = _stalls.GetValueOrDefault(buyer.Id); }
        if (s == null) return false;

        if (!s.Escrowed && zenyLimit > 0)
        {
            var session = _sessions?.GetByEntityId(buyer.Id);
            if (session?.CharacterData == null) return false;
            if ((long)session.CharacterData.Zeny < zenyLimit) // rAthena: buyer must back the full limit
            {
                Close(buyer);
                return false;
            }
            session.CharacterData.Zeny = (uint)((long)session.CharacterData.Zeny - zenyLimit);
            s.ZenyHeld = zenyLimit;
            s.Escrowed = true;
        }
        s.Title = title;
        s.Offers = offers.ToList();
        s.X = buyer.X; s.Y = buyer.Y; s.MapId = buyer.MapId;
        return true;
    }

    /// <summary>FEATURE-12 — close + **refund the buyer's unspent escrow** (rAthena buyingstore_close).</summary>
    public void Close(PlayerEntity buyer)
    {
        if (!_stalls.Remove(buyer.Id, out var stall)) return;
        _accountIndex.Remove(buyer.AccountId);
        Refund(stall, _sessions?.GetByEntityId(buyer.Id));
    }

    public void Reopen(PlayerEntity buyer)
        => _logger.LogInformation("buyingstore_reopen: autotrade rehydrate for {Buyer} (persistence → FEATURE-36)", buyer.Name);

    /// <summary>
    /// FEATURE-12 — rAthena <c>buyingstore_trade</c>: a seller sells items into the store. Validates
    /// the store id (anti-desync) + every item (seller stock, a live offer wants it, the held escrow
    /// covers it, buyer inventory space) BEFORE any mutation, then transfers the item (seller → buyer)
    /// and pays the seller from the held escrow. Auto-closes (+ refunds the remainder) when the escrow
    /// is spent or every offer is filled. Returns false (no partial transfer) on any gate failure.
    /// </summary>
    public bool Trade(PlayerEntity seller, int buyerAccountId, uint storeId,
        IReadOnlyList<(short index, short amount)> items)
    {
        if (!_accountIndex.TryGetValue(buyerAccountId, out var bid)) return false;
        if (!_stalls.TryGetValue(bid, out var stall)) return false;
        if (stall.StoreId != storeId) return false; // stale store id

        var sellerSession = _sessions?.GetByEntityId(seller.Id);
        var buyerSession = _sessions?.GetByAccountId(buyerAccountId);
        var sellerInv = sellerSession?.Inventory;
        var buyerInv = buyerSession?.Inventory;
        if (sellerSession?.CharacterData == null || sellerInv == null) return false;
        if (buyerSession?.CharacterData == null || buyerInv == null) return false;

        // Pass 1 — validate the whole request + plan the transfer.
        var plan = new List<(InventoryItem sellerItem, int offerSlot, int amount, long total)>();
        long heldRemaining = stall.ZenyHeld;
        var offerLeft = stall.Offers.Select(o => (int)o.amount).ToArray();
        var buyerFreshSlots = 0;
        foreach (var (idx, amount) in items)
        {
            if (amount <= 0) return false;
            var sellerItem = FindBySlot(sellerInv, idx);
            if (sellerItem == null || sellerItem.Equip != 0 || sellerItem.Amount < (uint)amount) return false;
            var offerSlot = -1;
            for (var i = 0; i < stall.Offers.Count; i++)
                if (stall.Offers[i].nameId == (int)sellerItem.NameId && offerLeft[i] >= amount) { offerSlot = i; break; }
            if (offerSlot < 0) return false; // no live offer wants this item/amount
            var total = (long)stall.Offers[offerSlot].price * amount;
            if (total > heldRemaining) return false; // escrow can't cover it
            heldRemaining -= total;
            offerLeft[offerSlot] -= amount;
            if (FindMergeable(buyerInv, sellerItem) == null) buyerFreshSlots++;
            plan.Add((sellerItem, offerSlot, amount, total));
        }
        if (buyerInv.Count + buyerFreshSlots > MaxInventory) return false; // buyer can't hold the goods

        // Pass 2 — transfer (all-or-nothing).
        foreach (var (sellerItem, offerSlot, amount, total) in plan)
        {
            DebitSlot(sellerSession, sellerInv, sellerItem, amount);   // seller loses the item
            CreditItem(buyerInv, sellerItem, amount);                  // buyer gains it
            sellerSession.CharacterData.Zeny = (uint)((long)sellerSession.CharacterData.Zeny + total); // seller paid from escrow
            stall.ZenyHeld -= total;
            var (nid, amt, price) = stall.Offers[offerSlot];
            stall.Offers[offerSlot] = (nid, (short)(amt - amount), price);
        }
        // PACKET-* (CZ/ZC buying-store): ZC_UPDATE_ITEM_FROM_BUYING_STORE + delete emit seam.
        _logger.LogInformation("buyingstore_trade: {Seller} sold {N} item(s) into acc {Buyer}'s store (held {Held})",
            seller.Name, plan.Count, buyerAccountId, stall.ZenyHeld);

        // Auto-close + refund when the escrow is spent or every offer is filled.
        if (stall.ZenyHeld <= 0 || stall.Offers.All(o => o.amount <= 0))
        {
            _stalls.Remove(bid);
            _accountIndex.Remove(buyerAccountId);
            Refund(stall, buyerSession);
        }
        return true;
    }

    public bool Search(PlayerEntity searcher, int nameId)
    {
        foreach (var stall in _stalls.Values)
            foreach (var (id, amt, _) in stall.Offers)
                if (id == nameId && amt > 0) return true;
        return false;
    }

    public bool SearchAll(PlayerEntity searcher, int nameId)
    {
        var count = 0;
        foreach (var stall in _stalls.Values)
            foreach (var (id, amt, _) in stall.Offers)
                if (id == nameId && amt > 0) count++;
        return count > 0;
    }

    /// <summary>rAthena <c>buyingstore_autotrade_init</c>. ➡️ The persisted offline-buyer loader (EF
    /// entity + repo + escrow + NPC) is FEATURE-36.</summary>
    public void InitAutotrade()
        => _logger.LogInformation("buyingstore_autotrade_init: 0 offline buyers hydrated (persistence → FEATURE-36)");

    /// <summary>FEATURE-12 test seam — the open store's id (anti-desync token the seller echoes back).</summary>
    internal uint? StoreIdOf(EntityId buyerId) => _stalls.TryGetValue(buyerId, out var s) ? s.StoreId : null;

    // --- helpers ---

    private static void Refund(BuyStall stall, Map.Server.MapSessionData? buyerSession)
    {
        if (stall.ZenyHeld <= 0 || buyerSession?.CharacterData == null) return;
        buyerSession.CharacterData.Zeny = (uint)((long)buyerSession.CharacterData.Zeny + stall.ZenyHeld);
        stall.ZenyHeld = 0;
    }

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

    private static void CreditItem(List<InventoryItem> inv, InventoryItem src, int qty)
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

    private static void DebitSlot(Map.Server.MapSessionData session, List<InventoryItem> inv, InventoryItem item, int qty)
    {
        item.Amount -= (uint)qty;
        if (item.Amount == 0)
        {
            if (item.Id > 0) session.RemovedInventoryIds.Add(item.Id);
            inv.Remove(item);
        }
    }

    private sealed class BuyStall
    {
        public EntityId BuyerId;
        public int BuyerAccountId;
        public uint StoreId;
        public byte EffectId;
        public string Title = "";
        public long ZenyHeld;     // escrowed zeny held in the store (decrements on trade, refunded on close).
        public bool Escrowed;
        public List<(int nameId, short amount, int price)> Offers = new();
        public short X;
        public short Y;
        public uint MapId;
    }
}
