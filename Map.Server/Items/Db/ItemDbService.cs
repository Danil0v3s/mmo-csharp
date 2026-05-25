using Map.Server.Entities;
using Map.Server.World;
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
    private readonly IRandomOptionService? _randomOpt;
    private readonly IRouletteService? _roulette;
    private readonly IMapFlagService? _mapFlags;
    private readonly IMapWorldRegistry? _maps;
    private readonly ILogger<ItemDbService> _logger;

    public ItemDbService(
        ILogger<ItemDbService> logger,
        IItemCatalog? catalog = null,
        IRandomOptionService? randomOpt = null,
        IMapFlagService? mapFlags = null,
        IMapWorldRegistry? maps = null,
        IRouletteService? roulette = null)
    {
        _logger = logger;
        _catalog = catalog;
        _randomOpt = randomOpt;
        _mapFlags = mapFlags;
        _maps = maps;
        _roulette = roulette;
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
    /// rAthena <c>itemdb_isNoEquip</c> (<c>itemdb.cpp:4442</c>) — bit
    /// 1=normal, 2=PVP, 4=GVG, 8=BG, 16=WOE:TE on the item's
    /// <c>nouse_override</c> column. Returns true when the item's
    /// bitmap forbids equip on the given map kind.
    ///
    /// <para>The bit / map-kind matrix:</para>
    /// <list type="bullet">
    ///   <item>bit 1 (Normal) → map is NOT pvp/gvg/bg/woe.</item>
    ///   <item>bit 2 (PVP)    → map has a PvP mapflag (we infer
    ///         "PvP-enabled" as the absence of <see cref="MapFlag.NoPvp"/>
    ///         on a non-GvG map — the catalog has no dedicated Pvp flag
    ///         yet; this is a conservative read that matches the
    ///         baseline behavior).</item>
    ///   <item>bit 4 (GVG)    → <see cref="MapFlag.Gvg"/> on the map.</item>
    ///   <item>bits 8 (BG) / 16 (WOE:TE) — flags not yet exposed by
    ///         <see cref="IMapFlagService"/>; the gate is skipped
    ///         silently (the corresponding bits never fire). The C# port
    ///         picks up these branches once those flags surface.</item>
    /// </list>
    /// </summary>
    public bool IsNoEquip(int itemId, uint mapId)
    {
        var row = _catalog?.Get((uint)itemId);
        var nouse = row?.NouseOverride ?? 0;
        if (nouse == 0) return false; // most-common short-circuit
        if (_mapFlags == null || _maps == null) return false;

        var mapName = ResolveMapName(mapId);
        if (string.IsNullOrEmpty(mapName)) return false;

        bool gvg = _mapFlags.IsSet(mapName, MapFlag.Gvg);
        // Pvp inferred as "not no-pvp and not gvg". rAthena evaluates
        // the raw MF_PVP cell flag; we approximate via the available
        // negative flag until MF_PVP lands on IMapFlagService.
        bool pvp = !gvg && !_mapFlags.IsSet(mapName, MapFlag.NoPvp);
        // Normal = none of pvp/gvg/bg/woe set on the map.
        bool normal = !pvp && !gvg;

        if ((nouse & 0x01) != 0 && normal) return true;
        if ((nouse & 0x02) != 0 && pvp) return true;
        if ((nouse & 0x04) != 0 && gvg) return true;
        // Bits 0x08 (BG) / 0x10 (WOE:TE) — IMapFlagService doesn't surface
        // these yet; the gate is skipped silently.
        return false;
    }

    private string ResolveMapName(uint mapId)
    {
        if (_maps == null) return string.Empty;
        foreach (var m in _maps.All)
        {
            if ((uint)m.Name.GetHashCode() == mapId) return m.Name;
        }
        return string.Empty;
    }

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

    /// <summary>
    /// rAthena <c>item_data::isStackable</c>. Same Weapon/Armor/PetEgg/
    /// PetArmor/Shadowgear exclusion as <see cref="IsStackable2"/>.
    /// </summary>
    public bool IsStackable(int itemId) => IsStackable2(itemId);

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

    /// <summary>
    /// rAthena <c>item_data::inventorySlotNeeded</c>. Stackable items
    /// (consumables / cards / aether / ammo) collapse to 1 slot
    /// regardless of quantity; weapons / armor / pet eggs / shadow gear
    /// each need their own row. <c>IsStackable2</c> handles the type
    /// classification.
    /// </summary>
    public int InventorySlotNeeded(int itemId, int quantity)
    {
        if (quantity < 0) quantity = 0;
        return IsStackable2(itemId) ? 1 : quantity;
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
    public void Reload()
    {
        _catalog?.Reload();
        _randomOpt?.Reload();
        _roulette?.Reload();
    }

    /// <summary>
    /// rAthena <c>itemdb_gen_itemmoveinfo</c> (<c>itemdb.cpp:4949</c>) —
    /// emits the <c>itemmoveinfov5.txt</c> client side-data dump:
    /// one row per non-default-trade-restriction item with the eight
    /// trade flags + a name comment. Used by /itemreset client tooling.
    ///
    /// <para>The output path mirrors rAthena's
    /// <c>./generated/clientside/data/itemmoveinfov5.txt</c> relative to
    /// the working directory; callers (GM command, boot dump option)
    /// can override via <paramref name="outputPath"/>.</para>
    /// </summary>
    public void GenItemMoveInfo(string? outputPath = null)
    {
        if (_catalog == null) return;
        var path = string.IsNullOrEmpty(outputPath)
            ? System.IO.Path.Combine("generated", "clientside", "data", "itemmoveinfov5.txt")
            : outputPath;
        try
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);

            using var w = new System.IO.StreamWriter(path, append: false);
            w.WriteLine("// ItemID,\tDrop,\tVending,\tStorage,\tCart,\tNpc Sale,\tMail,\tAuction,\tGuildStorage");
            w.WriteLine("// This format does not accept blank lines. Be careful.");

            int written = 0;
            foreach (var row in _catalog.All().OrderBy(r => r.Id))
            {
                if (row.Type == null) continue;
                // rAthena: skip if every trade restriction is default 0.
                var drop = row.TradeNodrop ?? 0;
                var trade = row.TradeNotrade ?? 0;
                var storage = row.TradeNostorage ?? 0;
                var cart = row.TradeNocart ?? 0;
                var sell = row.TradeNosell ?? 0;
                var mail = row.TradeNomail ?? 0;
                var auction = row.TradeNoauction ?? 0;
                var guildStorage = row.TradeNoguildstorage ?? 0;
                if ((drop | trade | storage | cart | sell | mail | auction | guildStorage) == 0) continue;

                w.Write(row.Id);
                w.Write('\t'); w.Write(drop);
                w.Write('\t'); w.Write(trade);
                w.Write('\t'); w.Write(storage);
                w.Write('\t'); w.Write(cart);
                w.Write('\t'); w.Write(sell);
                w.Write('\t'); w.Write(mail);
                w.Write('\t'); w.Write(auction);
                w.Write('\t'); w.Write(guildStorage);
                w.Write("\t// "); w.WriteLine(row.NameAegis);
                written++;
            }
            _logger.LogInformation(
                "itemdb_gen_itemmoveinfo: wrote {Rows} row(s) to {Path}",
                written, path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "itemdb_gen_itemmoveinfo failed for {Path}", path);
        }
    }

    // Interface-shape overload: matches IItemDbService.GenItemMoveInfo().
    void IItemDbService.GenItemMoveInfo() => GenItemMoveInfo(null);

    /// <summary>
    /// rAthena <c>itemdb_parse_roulette_db</c> — delegates to
    /// <see cref="IRouletteService.Reload"/>. Returns true if at least
    /// one row loaded. The seeded SQL lives in
    /// <c>seed_roulette_default_data.sql</c>.
    /// </summary>
    public bool ParseRouletteDb() => _roulette?.Reload() ?? false;
    public byte GetItemGroup(int groupId, PlayerEntity pc) => 0;
    public ushort FindComboId(IReadOnlyList<int> equippedItems) => 0;

    /// <summary>
    /// rAthena <c>s_random_opt_group::apply</c> — delegates to
    /// <see cref="IRandomOptionService.Apply"/>. The 5-slot result is
    /// appended to <paramref name="output"/>; callers can copy directly
    /// onto <c>InventoryItem.Options</c>.
    /// </summary>
    public void ApplyRandomOptionGroup(int groupId, IList<(int id, int value, int param)> output)
        => _randomOpt?.Apply(groupId, output);

    /// <summary>rAthena <c>RandomOptionDatabase::option_exists</c>.</summary>
    public bool RandomOptionExists(int optionId)
        => _randomOpt?.OptionExists(optionId) ?? false;

    /// <summary>rAthena <c>RandomOptionDatabase::option_get_id</c>.</summary>
    public int RandomOptionGetId(string optionName)
        => _randomOpt?.OptionGetId(optionName) ?? 0;
}
