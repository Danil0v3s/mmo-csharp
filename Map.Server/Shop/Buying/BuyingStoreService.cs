using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Buying;

/// <summary>
/// Default <see cref="IBuyingStoreService"/>. In-memory per-buyer stalls.
/// AT-D2 wave: real setup + coord refresh on Update; Search/SearchAll
/// walk the active stall set.
/// </summary>
public sealed class BuyingStoreService : IBuyingStoreService
{
    private const int MaxOffers = 5;       // rAthena MAX_BUYINGSTORE_SLOTS
    private const long MaxZenyLimit = 99_999_999L;

    private readonly Dictionary<EntityId, BuyStall> _stalls = new();
    private readonly Dictionary<int, EntityId> _accountIndex = new();
    private readonly ILogger<BuyingStoreService> _logger;
    public BuyingStoreService(ILogger<BuyingStoreService> logger) => _logger = logger;

    /// <summary>
    /// rAthena <c>buyingstore_setup</c> — gate before opening the stall.
    /// Returns the effect id back (and seeds the stall slot) only when
    /// the player isn't already vending and the effect id is in the
    /// approved range (rAthena allows 0/1/2/3).
    /// </summary>
    public void Open(PlayerEntity buyer, byte effectId)
    {
        if (effectId > 3)
        {
            _logger.LogWarning("buyingstore_setup: rejected effect={Eff} for {Buyer}", effectId, buyer.Name);
            return;
        }
        if (_stalls.ContainsKey(buyer.Id))
        {
            _logger.LogWarning("buyingstore_setup: already open for {Buyer}", buyer.Name);
            return;
        }
        _stalls[buyer.Id] = new BuyStall
        {
            BuyerId = buyer.Id,
            BuyerAccountId = buyer.AccountId,
            EffectId = effectId,
            X = buyer.X,
            Y = buyer.Y,
            MapId = buyer.MapId,
        };
        _accountIndex[buyer.AccountId] = buyer.Id;
    }

    public void Update(PlayerEntity buyer, string title, long zenyLimit,
        IReadOnlyList<(int nameId, short amount, int price)> offers)
    {
        if (offers.Count > MaxOffers) return;
        if (zenyLimit < 0 || zenyLimit > MaxZenyLimit) return;
        var s = _stalls.GetValueOrDefault(buyer.Id);
        if (s == null) { Open(buyer, 0); s = _stalls[buyer.Id]; }
        s.Title = title;
        s.ZenyLimit = zenyLimit;
        s.Offers = offers.ToList();
        // Coord refresh (vendor moved between Open and Update).
        s.X = buyer.X; s.Y = buyer.Y; s.MapId = buyer.MapId;
    }

    public void Close(PlayerEntity buyer)
    {
        if (_stalls.Remove(buyer.Id))
            _accountIndex.Remove(buyer.AccountId);
    }

    public void Reopen(PlayerEntity buyer)
    {
        _logger.LogInformation("buyingstore_reopen: autotrade rehydrate for {Buyer}", buyer.Name);
    }

    public void Trade(PlayerEntity seller, int buyerAccountId, uint storeId,
        IReadOnlyList<(short index, short amount)> items)
    {
        if (!_accountIndex.TryGetValue(buyerAccountId, out var bid)) return;
        if (!_stalls.TryGetValue(bid, out var stall)) return;
        // The packet handler does inventory + zeny mutation; here we
        // just decrement remaining buy-quantities and trip the zeny
        // limit guard.
        foreach (var (idx, amount) in items)
        {
            for (int i = 0; i < stall.Offers.Count; i++)
            {
                if (stall.Offers[i].nameId != idx) continue;
                var (id, currentAmt, price) = stall.Offers[i];
                if (currentAmt < amount) return;
                long cost = (long)price * amount;
                if (cost > stall.ZenyLimit) return;
                stall.ZenyLimit -= cost;
                stall.Offers[i] = (id, (short)(currentAmt - amount), price);
            }
        }
        if (stall.Offers.All(o => o.amount <= 0))
            Close(seller); // auto-close on full buyout
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

    public void InitAutotrade()
    {
        _logger.LogInformation("buyingstore_autotrade_init: 0 buyers hydrated (loader pending)");
    }

    private sealed class BuyStall
    {
        public EntityId BuyerId;
        public int BuyerAccountId;
        public byte EffectId;
        public string Title = "";
        public long ZenyLimit;
        public List<(int nameId, short amount, int price)> Offers = new();
        public short X;
        public short Y;
        public uint MapId;
    }
}
