using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop.Vending;

/// <summary>
/// Default <see cref="IVendingService"/>. Tracks per-vendor stall
/// state in-memory; persistence (`autotrade_db`) data-pending. Wire
/// packets land alongside their consumers.
/// </summary>
public sealed class VendingService : IVendingService
{
    private readonly Dictionary<EntityId, Stall> _stalls = new();
    private readonly ILogger<VendingService> _logger;
    public VendingService(ILogger<VendingService> logger) => _logger = logger;

    public void Update(PlayerEntity vendor, string title, IReadOnlyList<(short index, short qty, int price)> items)
        => _stalls[vendor.Id] = new Stall { Title = title, Items = items.ToList() };

    public void CloseVending(PlayerEntity vendor) => _stalls.Remove(vendor.Id);
    public void Reopen(PlayerEntity vendor) { /* autotrade table data-pending */ }
    public void VendingListReq(PlayerEntity buyer, int vendorAccountId) { }
    public void PurchaseReq(PlayerEntity buyer, int vendorAccountId, IReadOnlyList<(short index, short qty)> items) { }
    public bool Search(PlayerEntity searcher, int nameId) => false;
    public bool SearchAll(PlayerEntity searcher, int nameId) => false;
    public void InitAutotrade() { }

    private sealed class Stall
    {
        public string Title = "";
        public List<(short index, short qty, int price)> Items = new();
    }
}
