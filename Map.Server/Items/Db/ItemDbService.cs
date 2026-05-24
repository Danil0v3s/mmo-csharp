using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Items.Db;

/// <summary>
/// Default <see cref="IItemDbService"/>. Gate predicates read the
/// per-item <c>TradeNo*</c> columns from <see cref="IItemCatalog"/>
/// (hydrated from <c>item_db</c> at boot) — matches rAthena's
/// `itemdb_can*_sub` family in <c>itemdb.cpp</c>. Missing rows default
/// to permissive (true) so legacy paths that pre-date the catalog
/// continue to work.
/// </summary>
public sealed class ItemDbService : IItemDbService
{
    private readonly IItemCatalog? _catalog;
    private readonly ILogger<ItemDbService> _logger;

    public ItemDbService(ILogger<ItemDbService> logger, IItemCatalog? catalog = null)
    {
        _logger = logger;
        _catalog = catalog;
    }

    public bool CanTrade(int itemId, PlayerEntity pc)
        => (_catalog?.Get((uint)itemId)?.TradeNotrade ?? 0) == 0;

    public bool CanPartnerTrade(int itemId, PlayerEntity pc)
        => (_catalog?.Get((uint)itemId)?.TradeTradepartner ?? 0) == 0;

    public bool CanSell(int itemId, PlayerEntity pc)
        => (_catalog?.Get((uint)itemId)?.TradeNosell ?? 0) == 0;

    public bool CanStore(int itemId, PlayerEntity pc)
        => (_catalog?.Get((uint)itemId)?.TradeNostorage ?? 0) == 0;

    public bool CanCartStore(int itemId, PlayerEntity pc)
        => (_catalog?.Get((uint)itemId)?.TradeNocart ?? 0) == 0;

    public bool CanGuildStore(int itemId, PlayerEntity pc)
        => (_catalog?.Get((uint)itemId)?.TradeNoguildstorage ?? 0) == 0;

    public bool CanMail(int itemId, PlayerEntity pc)
        => (_catalog?.Get((uint)itemId)?.TradeNomail ?? 0) == 0;

    public bool CanAuction(int itemId, PlayerEntity pc)
        => (_catalog?.Get((uint)itemId)?.TradeNoauction ?? 0) == 0;

    public bool IsDroppable(int itemId, PlayerEntity pc)
        => (_catalog?.Get((uint)itemId)?.TradeNodrop ?? 0) == 0;

    /// <summary>
    /// rAthena <c>itemdb_isrestricted</c> — any TradeOverride or
    /// flag in the trade column is set. Returns true when the item
    /// has at least one non-default trade restriction.
    /// </summary>
    public bool IsRestricted(int itemId, PlayerEntity pc)
    {
        var row = _catalog?.Get((uint)itemId);
        if (row == null) return false;
        return (row.TradeOverride ?? 0) != 0
            || (row.TradeNodrop ?? 0) != 0
            || (row.TradeNotrade ?? 0) != 0
            || (row.TradeTradepartner ?? 0) != 0
            || (row.TradeNosell ?? 0) != 0
            || (row.TradeNocart ?? 0) != 0
            || (row.TradeNostorage ?? 0) != 0
            || (row.TradeNoguildstorage ?? 0) != 0
            || (row.TradeNomail ?? 0) != 0
            || (row.TradeNoauction ?? 0) != 0;
    }

    /// <summary>
    /// rAthena <c>itemdb_isNoEquip</c> — checks `nouse` mapflag.
    /// The map-side flag check lives in IMapFlagService; the per-item
    /// nouse column isn't currently on the catalog (rAthena's
    /// nouse_override + nouse_sitting are bitmap flags). First-slice
    /// returns false (no map gates equip by item id) — when the column
    /// surfaces, the map-id check threads through here.
    /// </summary>
    public bool IsNoEquip(int itemId, uint mapId) => false;

    /// <summary>True if the item is equip-class (weapon/armor/headgear/etc.).</summary>
    public bool IsEquip2(int itemId)
    {
        var row = _catalog?.Get((uint)itemId);
        if (row == null) return false;
        // rAthena IT_WEAPON=4, IT_ARMOR=5. Our catalog stores Type as a string.
        return row.Type == "Weapon" || row.Type == "Armor";
    }

    /// <summary>
    /// True if the item stacks. rAthena's `itemdb_isstackable2`
    /// returns true for everything that isn't Equip / Pet Egg /
    /// Pet Armor / Shadow. Read from catalog's <c>Stack</c> column
    /// when present; otherwise inferred from <c>Type</c>.
    /// </summary>
    public bool IsStackable2(int itemId)
    {
        var row = _catalog?.Get((uint)itemId);
        if (row == null) return false;
        return row.Type != "Weapon"
            && row.Type != "Armor"
            && row.Type != "PetEgg"
            && row.Type != "PetArmor"
            && row.Type != "Shadowgear";
    }

    /// <summary>True if <paramref name="itemId"/> is a hatched pet egg item.</summary>
    public bool IsHatchedEgg(int itemId)
    {
        var row = _catalog?.Get((uint)itemId);
        return row?.Type == "PetEgg";
    }

    /// <summary>
    /// rAthena <c>itemdb_isidentified</c> — true if the item type
    /// auto-identifies on pickup (cards / consumables). Weapons /
    /// armor pickup unidentified by default.
    /// </summary>
    public bool IsIdentified(int itemId)
    {
        var row = _catalog?.Get((uint)itemId);
        if (row == null) return true;
        return row.Type != "Weapon" && row.Type != "Armor" && row.Type != "Shadowgear";
    }

    public int SearchNameArray(string namePattern, IList<int> output, int max)
    {
        if (_catalog == null || max <= 0) return 0;
        var matched = 0;
        foreach (var row in _catalog.All())
        {
            if (matched >= max) break;
            if (row.NameEnglish == null) continue;
            if (row.NameEnglish.Contains(namePattern, System.StringComparison.OrdinalIgnoreCase))
            {
                output.Add((int)row.Id);
                matched++;
            }
        }
        return matched;
    }

    /// <summary>
    /// Triggers a re-hydration of <see cref="IItemCatalog"/> from the
    /// SQL <c>item_db</c> snapshot. The catalog drives every per-item
    /// predicate above; reloading flushes the in-memory dictionary.
    /// </summary>
    public void Reload() => _catalog?.Reload();

    public void GenItemMoveInfo() { }
    public bool ParseRouletteDb() => false;
    public byte GetItemGroup(int groupId, PlayerEntity pc) => 0;
    public ushort FindComboId(IReadOnlyList<int> equippedItems) => 0;
    public void ApplyRandomOptionGroup(int groupId, IList<(int id, int value, int param)> output) { }
    public bool RandomOptionExists(int optionId) => false;
    public int RandomOptionGetId(string optionName) => 0;
}
