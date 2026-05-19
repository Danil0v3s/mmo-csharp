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
    /// <summary>rAthena <c>battle_config.min_shop_buy</c> default = 1z.</summary>
    private const int MinShopBuy = 1;
    /// <summary>rAthena <c>battle_config.min_shop_sell</c> default = 0z.</summary>
    private const int MinShopSell = 0;
    /// <summary>rAthena skill ids — see db/re/skill_db.yml.</summary>
    private const ushort MC_DISCOUNT = 37;
    private const ushort MC_OVERCHARGE = 38;
    private const ushort RG_COMPULSION = 224;

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
            // rAthena pc_modifybuyvalue (pc.cpp:5310) — Discount /
            // Compulsion reduce each line item independently.
            var effective = ModifyBuyValue(buyer, listing.Price);
            totalCost += (long)effective * amount;
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
            // Sell value = half the buy price + Overcharge bonus
            // (rAthena pc_modifysellvalue, pc.cpp:5331).
            var sellPrice = ModifySellValue(seller, buyPrice * SellRatioPercent / 100);
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

    /// <summary>
    /// Port of rAthena <c>pc_modifybuyvalue</c> (pc.cpp:5310). Discount
    /// (MC_DISCOUNT) and Compulsion (RG_COMPULSION) discounts apply at
    /// the higher of the two rates. Clamped to <c>battle.min_shop_buy</c>.
    /// </summary>
    private static int ModifyBuyValue(PlayerEntity pc, int orig)
    {
        var rate1 = 0;
        var rate2 = 0;
        var discount = pc.LearnedSkills.GetValueOrDefault(MC_DISCOUNT);
        if (discount > 0) rate1 = 5 + discount * 2 - (discount == 10 ? 1 : 0);
        var compulsion = pc.LearnedSkills.GetValueOrDefault(RG_COMPULSION);
        if (compulsion > 0) rate2 = 5 + compulsion * 4;
        var rate = Math.Max(rate1, rate2);
        var val = rate == 0 ? orig : (int)(orig * (100 - rate) / 100.0);
        return Math.Max(MinShopBuy, val);
    }

    /// <summary>
    /// Port of rAthena <c>pc_modifysellvalue</c> (pc.cpp:5331).
    /// Overcharge (MC_OVERCHARGE) boosts the sell price linearly.
    /// </summary>
    private static int ModifySellValue(PlayerEntity pc, int orig)
    {
        var overcharge = pc.LearnedSkills.GetValueOrDefault(MC_OVERCHARGE);
        if (overcharge == 0) return Math.Max(MinShopSell, orig);
        var rate = 5 + overcharge * 2 - (overcharge == 10 ? 1 : 0);
        return Math.Max(MinShopSell, (int)(orig * (100 + rate) / 100.0));
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
