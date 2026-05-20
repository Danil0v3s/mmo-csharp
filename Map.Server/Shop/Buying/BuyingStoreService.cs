using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Buying;

/// <summary>Default <see cref="IBuyingStoreService"/>. In-memory per-buyer stalls.</summary>
public sealed class BuyingStoreService : IBuyingStoreService
{
    private readonly Dictionary<EntityId, BuyStall> _stalls = new();
    private readonly ILogger<BuyingStoreService> _logger;
    public BuyingStoreService(ILogger<BuyingStoreService> logger) => _logger = logger;

    public void Open(PlayerEntity buyer, byte effectId)
        => _stalls[buyer.Id] = new BuyStall();

    public void Update(PlayerEntity buyer, string title, long zenyLimit,
        IReadOnlyList<(int nameId, short amount, int price)> offers)
    {
        var s = _stalls.GetValueOrDefault(buyer.Id) ?? (_stalls[buyer.Id] = new BuyStall());
        s.Title = title; s.ZenyLimit = zenyLimit; s.Offers = offers.ToList();
    }

    public void Close(PlayerEntity buyer) => _stalls.Remove(buyer.Id);
    public void Reopen(PlayerEntity buyer) { }
    public void Trade(PlayerEntity seller, int buyerAccountId, uint storeId,
        IReadOnlyList<(short index, short amount)> items) { }
    public bool Search(PlayerEntity searcher, int nameId) => false;
    public bool SearchAll(PlayerEntity searcher, int nameId) => false;
    public void InitAutotrade() { }

    private sealed class BuyStall
    {
        public string Title = "";
        public long ZenyLimit;
        public List<(int nameId, short amount, int price)> Offers = new();
    }
}
