using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Scripting.Records;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Shop;

/// <summary>
/// First-slice <see cref="IShopService"/>. Validates buy lists against
/// the shop's <see cref="ShopRegistration.Items"/> + the buyer's zeny;
/// validates sell lists against the inventory + computes proceeds at
/// the rAthena default sell ratio (50% of the item_db buy price).
///
/// rAthena reference: npc.cpp:2762 npc_buylist (price validation +
/// inventory room check + zeny gate), npc.cpp:2997 npc_selllist
/// (slot validation + sell ratio).
/// </summary>
public sealed class ShopService : IShopService
{
    /// <summary>rAthena <c>battle_config.sell_ratio</c> default = 50%.</summary>
    private const int SellRatioPercent = 50;

    private readonly IItemCatalog _catalog;
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<ShopService> _logger;

    public ShopService(IItemCatalog catalog, ISessionManagerAccessor sessions, ILogger<ShopService> logger)
    {
        _catalog = catalog;
        _sessions = sessions;
        _logger = logger;
    }

    public ShopOpResult Buy(PlayerEntity buyer, ShopRegistration shop, IReadOnlyList<(int ItemId, int Amount)> items)
    {
        if (items.Count == 0) return ShopOpResult.InvalidQuantity;
        var session = _sessions.GetByEntityId(buyer.Id);
        if (session == null || session.CharacterData == null) return ShopOpResult.NotInShop;

        long totalCost = 0;
        foreach (var (itemId, amount) in items)
        {
            if (amount < 1) return ShopOpResult.InvalidQuantity;
            var listing = ShopListing(shop, itemId);
            if (listing == null) return ShopOpResult.InvalidSlot;
            totalCost += (long)listing.Price * amount;
        }
        if ((ulong)totalCost > session.CharacterData.Zeny) return ShopOpResult.NotEnoughZeny;

        // Deduct zeny + deposit items. Once we're past validation no
        // partial commits — match rAthena's all-or-nothing behavior.
        session.CharacterData.Zeny -= (uint)totalCost;
        session.Inventory ??= new List<InventoryItem>();
        foreach (var (itemId, amount) in items)
        {
            DepositItem(session, (uint)itemId, amount);
        }
        _logger.LogInformation(
            "Char {Char} bought {Count} item types from {Shop} for {Zeny} zeny",
            buyer.CharacterId, items.Count, shop.Name, totalCost);
        return ShopOpResult.Ok;
    }

    public ShopOpResult Sell(PlayerEntity seller, IReadOnlyList<(int InventoryIndex, int Amount)> items)
    {
        if (items.Count == 0) return ShopOpResult.InvalidQuantity;
        var session = _sessions.GetByEntityId(seller.Id);
        if (session?.Inventory is not { } inv || session.CharacterData == null)
            return ShopOpResult.NotInShop;

        // Validate every slot up-front; rAthena rejects the whole list on
        // any invalid entry.
        long totalProceeds = 0;
        foreach (var (slot, amount) in items)
        {
            if (amount < 1) return ShopOpResult.InvalidQuantity;
            if (slot < 0 || slot >= inv.Count) return ShopOpResult.InvalidSlot;
            if (inv[slot].Amount < amount) return ShopOpResult.InvalidQuantity;
            var row = _catalog.Get(inv[slot].NameId);
            if (row == null) return ShopOpResult.InvalidSlot;
            var buyPrice = (int)(row.PriceBuy ?? 0);
            // Sell value = half the buy price (rAthena pc_modifysellvalue).
            var sellPrice = buyPrice * SellRatioPercent / 100;
            totalProceeds += (long)sellPrice * amount;
        }

        // Mutate after validation passes — descending slot order so
        // RemoveAt doesn't shift entries we haven't processed yet.
        foreach (var (slot, amount) in items.OrderByDescending(t => t.InventoryIndex))
        {
            var src = inv[slot];
            src.Amount -= (uint)amount;
            if (src.Amount == 0)
            {
                if (src.Id > 0) session.RemovedInventoryIds.Add(src.Id);
                inv.RemoveAt(slot);
            }
        }
        session.CharacterData.Zeny = (uint)Math.Min(uint.MaxValue, session.CharacterData.Zeny + (ulong)totalProceeds);
        _logger.LogInformation(
            "Char {Char} sold {Count} entries for {Zeny} zeny",
            seller.CharacterId, items.Count, totalProceeds);
        return ShopOpResult.Ok;
    }

    private static ShopItem? ShopListing(ShopRegistration shop, int itemId)
    {
        foreach (var item in shop.Items)
        {
            if (item.ItemId == itemId) return item;
        }
        return null;
    }

    private static void DepositItem(MapSessionData session, uint nameId, int amount)
    {
        var inv = session.Inventory!;
        foreach (var i in inv)
        {
            if (i.NameId == nameId && i.Refine == 0 && i.Card0 == 0 && i.Card1 == 0 && i.Card2 == 0 && i.Card3 == 0)
            {
                i.Amount += (uint)amount;
                return;
            }
        }
        inv.Add(new InventoryItem
        {
            ServerIndex = inv.Count,
            NameId = nameId,
            Amount = (uint)amount,
            Identified = true,
        });
    }
}
