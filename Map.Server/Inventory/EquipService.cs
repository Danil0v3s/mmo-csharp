using Map.Server.Entities;
using Map.Server.Inventory.Script;
using Map.Server.Items;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Inventory;

/// <summary>
/// First-slice <see cref="IEquipService"/>. Port of rAthena
/// <c>pc_equipitem</c> / <c>pc_unequipitem</c> covering weapon,
/// shield, armor, headgear, garment, shoes, accessories, ammo. After
/// every wear-state change drives a fresh <see cref="IStatusCalcService.CalcPc"/>
/// using <see cref="EquipBonusAggregator"/> for the equipment-derived
/// portion of <see cref="PcBaseInputs"/>.
/// </summary>
public sealed class EquipService : IEquipService
{
    private readonly IItemCatalog _catalog;
    private readonly IEntityRegistry _entities;
    private readonly IStatusCalcService _statusCalc;
    private readonly IItemCombosService? _combos;
    private readonly IScriptedBonusService? _scriptedBonuses;
    private readonly IComboDispatcher? _comboDispatcher;
    private readonly IItemHookDispatcher? _hookDispatcher;
    private readonly ILogger<EquipService> _logger;

    public EquipService(
        IItemCatalog catalog,
        IEntityRegistry entities,
        IStatusCalcService statusCalc,
        ILogger<EquipService> logger,
        IItemCombosService? combos = null,
        IScriptedBonusService? scriptedBonuses = null,
        IComboDispatcher? comboDispatcher = null,
        IItemHookDispatcher? hookDispatcher = null)
    {
        _catalog = catalog;
        _entities = entities;
        _statusCalc = statusCalc;
        _combos = combos;
        _scriptedBonuses = scriptedBonuses;
        _comboDispatcher = comboDispatcher;
        _hookDispatcher = hookDispatcher;
        _logger = logger;
    }

    public EquipOpResult Equip(MapSessionData session, int serverIndex, uint requestedPos, out uint appliedPos)
    {
        appliedPos = 0;
        if (session.Inventory is not { } inv) return EquipOpResult.NoSuchItem;
        if (serverIndex < 0 || serverIndex >= inv.Count) return EquipOpResult.NoSuchItem;
        var item = inv[serverIndex];
        if (item.NameId == 0 || item.Amount == 0) return EquipOpResult.NoSuchItem;
        if (!item.Identified) return EquipOpResult.NotIdentified;

        var row = _catalog.Get(item.NameId);
        if (row == null) return EquipOpResult.NotEquippable;
        var allowed = ResolveAllowedPositions(row);
        if (allowed == 0) return EquipOpResult.NotEquippable;

        // rAthena pc_equipitem: server intersects the item's allowed
        // bitmask with the client's request, then resolves ambiguous
        // ACC / dual-wield cases.
        var pos = ResolveTargetSlot(inv, allowed, requestedPos);
        if (pos == 0 || (pos & allowed) == 0) return EquipOpResult.InvalidSlot;

        // Drop anything in the slot bits we're about to occupy.
        for (var i = 0; i < inv.Count; i++)
        {
            if (i == serverIndex) continue;
            if ((inv[i].Equip & pos) != 0)
            {
                inv[i].Equip = 0;
            }
        }
        item.Equip = pos;
        appliedPos = pos;

        TryRecalcStats(session);
        return EquipOpResult.Ok;
    }

    public EquipOpResult Unequip(MapSessionData session, int serverIndex, out uint clearedPos)
    {
        clearedPos = 0;
        if (session.Inventory is not { } inv) return EquipOpResult.NoSuchItem;
        if (serverIndex < 0 || serverIndex >= inv.Count) return EquipOpResult.NoSuchItem;
        var item = inv[serverIndex];
        if (item.Equip == 0) return EquipOpResult.InvalidSlot;

        clearedPos = item.Equip;

        // CONV-4: fire onUnequip BEFORE clearing the equip bit so the
        // hook still sees the item in its "equipped" state (relevant for
        // any ctx.getrefine() / ctx.getequiprefinerycnt() reads). Bundle
        // mutations from onUnequip are largely cosmetic (recalc rebuilds
        // from scratch immediately after), but ctx.player.* state
        // mutations persist — that's the intended use case (e.g. cards
        // that drain HP on unequip).
        if (_hookDispatcher != null && session.EntityId is { } uid
            && _entities.Get(uid) is PlayerEntity unequipPlayer)
        {
            var equipped = new List<InventoryItem>();
            for (var i = 0; i < inv.Count; i++)
                if (inv[i].Equip != 0) equipped.Add(inv[i]);
            _hookDispatcher.TryInvokeOnUnequip(item, unequipPlayer.EquipBonuses, unequipPlayer, equipped);
        }

        item.Equip = 0;

        TryRecalcStats(session);
        return EquipOpResult.Ok;
    }

    // --- helpers ---

    /// <summary>
    /// Build the EQP_* bitmask from the per-slot Location flags on the
    /// item_db row. rAthena does this once at item_db load
    /// (<c>itemdb_read</c>) and stashes it on <c>item_data.equip</c>;
    /// we recompute lazily because the catalog doesn't keep the
    /// combined value.
    /// </summary>
    private static uint ResolveAllowedPositions(Core.Database.Entities.ItemEntity row)
    {
        uint pos = 0;
        if (row.LocationHeadLow == 1) pos |= EquipBits.HeadLow;
        if (row.LocationHeadMid == 1) pos |= EquipBits.HeadMid;
        if (row.LocationHeadTop == 1) pos |= EquipBits.HeadTop;
        if (row.LocationRightHand == 1) pos |= EquipBits.HandR;
        if (row.LocationLeftHand == 1) pos |= EquipBits.HandL;
        if (row.LocationArmor == 1) pos |= EquipBits.Armor;
        if (row.LocationGarment == 1) pos |= EquipBits.Garment;
        if (row.LocationShoes == 1) pos |= EquipBits.Shoes;
        if (row.LocationRightAccessory == 1) pos |= EquipBits.AccR;
        if (row.LocationLeftAccessory == 1) pos |= EquipBits.AccL;
        if (row.LocationAmmo == 1) pos |= EquipBits.Ammo;
        // Costume overlay slots — rAthena item_db columns
        // location_costume_head_top / _mid / _low / _garment.
        if (row.LocationCostumeHeadTop == 1) pos |= EquipBits.CostumeHeadTop;
        if (row.LocationCostumeHeadMid == 1) pos |= EquipBits.CostumeHeadMid;
        if (row.LocationCostumeHeadLow == 1) pos |= EquipBits.CostumeHeadLow;
        if (row.LocationCostumeGarment == 1) pos |= EquipBits.CostumeGarment;
        // Shadow gear (rAthena late-game system).
        if (row.LocationShadowArmor == 1) pos |= EquipBits.ShadowArmor;
        if (row.LocationShadowWeapon == 1) pos |= EquipBits.ShadowWeapon;
        if (row.LocationShadowShield == 1) pos |= EquipBits.ShadowShield;
        if (row.LocationShadowShoes == 1) pos |= EquipBits.ShadowShoes;
        if (row.LocationShadowRightAccessory == 1) pos |= EquipBits.ShadowAccR;
        if (row.LocationShadowLeftAccessory == 1) pos |= EquipBits.ShadowAccL;
        return pos;
    }

    /// <summary>
    /// Pick the actual slot bits to use. Mirrors the ACC and ARMS
    /// resolution inside <c>pc_equipitem</c> (pc.cpp:11937).
    /// </summary>
    private static uint ResolveTargetSlot(IReadOnlyList<InventoryItem> inv, uint allowed, uint requested)
    {
        var pos = allowed & requested;

        // Accessory ambiguity — item allows both L+R; pick R if free, else L.
        if (allowed == EquipBits.AccRL)
        {
            pos = requested & EquipBits.AccRL;
            if (pos == EquipBits.AccRL)
                pos = SlotInUse(inv, EquipBits.AccR) && !SlotInUse(inv, EquipBits.AccL)
                    ? EquipBits.AccL
                    : EquipBits.AccR;
        }
        // Dual-wield: weapon allows both hand bits; pick L if R is taken (renewal).
        else if (allowed == EquipBits.Arms)
        {
            pos = requested & EquipBits.Arms;
            if (pos == EquipBits.Arms)
                pos = SlotInUse(inv, EquipBits.HandR) && !SlotInUse(inv, EquipBits.HandL)
                    ? EquipBits.HandL
                    : EquipBits.HandR;
        }
        // Shadow accessory ambiguity — same shape as ACC_RL.
        else if (allowed == EquipBits.ShadowAccRL)
        {
            pos = requested & EquipBits.ShadowAccRL;
            if (pos == EquipBits.ShadowAccRL)
                pos = SlotInUse(inv, EquipBits.ShadowAccR) && !SlotInUse(inv, EquipBits.ShadowAccL)
                    ? EquipBits.ShadowAccL
                    : EquipBits.ShadowAccR;
        }
        return pos;
    }

    private static bool SlotInUse(IReadOnlyList<InventoryItem> inv, uint bit)
    {
        for (var i = 0; i < inv.Count; i++) if ((inv[i].Equip & bit) != 0) return true;
        return false;
    }

    private void TryRecalcStats(MapSessionData session)
    {
        if (session.EntityId is not { } eid) return;
        if (_entities.Get(eid) is not PlayerEntity player) return;

        var summary = EquipBonusAggregator.Aggregate(session.Inventory, _catalog);

        // CONV-3: combo dispatch moved to IComboDispatcher, which walks
        // the TypeScript-registered combo set in INpcRegistry and fires
        // onActive hooks through the shared mmo-scripts V8 engine. When
        // the dispatcher is wired we skip the old IItemCombosService +
        // ActiveCombo + runtime-DSL path entirely — same data feeds both
        // (CONV-2 generated the .ts modules from item_combo_db), so the
        // dispatcher is the authoritative source. Without the dispatcher
        // we fall back to the legacy path so the surface stays viable in
        // tests that don't spin up a ScriptHost.
        IReadOnlyList<ActiveCombo>? activeCombos = null;
        if (_comboDispatcher == null && _combos != null)
        {
            activeCombos = _combos.RecomputeCombos(session);
            if (activeCombos.Count > 0)
            {
                _logger.LogDebug("item_combo: {N} legacy combo(s) firing for {Player} (ids: {Ids})",
                    activeCombos.Count, player.Name, string.Join(",", activeCombos.Select(c => c.ComboId)));
            }
        }

        // T2.2 — rebuild the indexed bonus bundle from each equip's
        // bonus script (regex pass over the item_db `script` column).
        // Read by IBattleCardService.CalcCardFix + cast / drain hooks.
        // DSL-4: scriptedBonuses + player thread the V8 bridge into the
        // build for the per-item dynamic-script fallback (CONV-4 will
        // move per-item dispatch onto the shared engine too).
        EquipBonusAggregator.BuildBundle(
            session.Inventory, _catalog, player.EquipBonuses, activeCombos,
            _scriptedBonuses, player, _hookDispatcher);

        // CONV-3: layer combo onActive bonuses on top of per-item bonuses
        // via the registry-driven dispatcher. Each combo's hook writes
        // into player.EquipBonuses in-place using the same host surface
        // (ScriptedBonusHost) the runtime DSL bridge uses, so downstream
        // combat / cast code sees combo bonuses indistinguishably from
        // per-item ones.
        if (_comboDispatcher != null)
        {
            var equipped = new List<InventoryItem>();
            for (var i = 0; i < session.Inventory!.Count; i++)
                if (session.Inventory[i].Equip != 0) equipped.Add(session.Inventory[i]);
            _comboDispatcher.ApplyActiveCombos(equipped, player.EquipBonuses, player);
        }
        var stats = player.Stats;
        _statusCalc.CalcPc(player, new PcBaseInputs(
            BaseLevel: player.Level,
            JobLevel: player.JobLevel,
            Str: stats.Str, Agi: stats.Agi, Vit: stats.Vit,
            Int: stats.IntStat, Dex: stats.Dex, Luk: stats.Luk,
            Pow: stats.Pow, Sta: stats.Sta, Wis: stats.Wis,
            Spl: stats.Spl, Con: stats.Con, Crt: stats.Crt,
            WeaponAtkMin: summary.WeaponAtkMin,
            WeaponAtkMax: summary.WeaponAtkMax,
            EquipDef: summary.EquipDef,
            EquipMdef: summary.EquipMdef,
            AttackRange: summary.AttackRange,
            WeaponElement: summary.WeaponElement));
    }
}

/// <summary>
/// EQP_* bit constants. Mirror rAthena <c>enum equip_pos</c>
/// (common/mmo.hpp:336). Covers core gear, costume (cosmetic-only
/// overlay), and shadow-gear (the late-game extra-slot system).
/// </summary>
public static class EquipBits
{
    // --- core gear (rAthena common/mmo.hpp:337-352) ---
    public const uint HeadLow = 0x000001;
    public const uint HandR   = 0x000002;
    public const uint Garment = 0x000004;
    public const uint AccR    = 0x000008;
    public const uint Armor   = 0x000010;
    public const uint HandL   = 0x000020;
    public const uint Shoes   = 0x000040;
    public const uint AccL    = 0x000080;
    public const uint HeadTop = 0x000100;
    public const uint HeadMid = 0x000200;

    // --- costume overlay (cosmetic only — no stat contribution) ---
    public const uint CostumeHeadTop = 0x000400;
    public const uint CostumeHeadMid = 0x000800;
    public const uint CostumeHeadLow = 0x001000;
    public const uint CostumeGarment = 0x002000;

    public const uint Ammo    = 0x008000;

    // --- shadow gear (rAthena 0x010000-0x200000) ---
    public const uint ShadowArmor  = 0x010000;
    public const uint ShadowWeapon = 0x020000;
    public const uint ShadowShield = 0x040000;
    public const uint ShadowShoes  = 0x080000;
    public const uint ShadowAccR   = 0x100000;
    public const uint ShadowAccL   = 0x200000;

    // --- combined ---
    public const uint Arms = HandR | HandL;
    public const uint AccRL = AccR | AccL;
    public const uint Helm = HeadLow | HeadMid | HeadTop;
    public const uint CostumeHelm = CostumeHeadLow | CostumeHeadMid | CostumeHeadTop;
    public const uint Costume = CostumeHelm | CostumeGarment;
    public const uint ShadowArms = ShadowWeapon | ShadowShield;
    public const uint ShadowAccRL = ShadowAccR | ShadowAccL;
    public const uint ShadowGear = ShadowArmor | ShadowWeapon | ShadowShield
                                 | ShadowShoes | ShadowAccR | ShadowAccL;
}
