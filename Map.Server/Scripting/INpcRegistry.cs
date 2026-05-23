using Map.Server.Scripting.Records;

namespace Map.Server.Scripting;

/// <summary>
/// In-memory catalog of every <c>register*()</c> call from the TS bundle.
/// Populated once during <see cref="ScriptHost.LoadEntryPoint"/>; consumed by
/// <see cref="NpcSpawnService"/> for entity placement, by future click
/// handlers for hook lookup, and by Phase 5's event dispatcher.
///
/// Indexes are built incrementally as <c>AddNpc</c> / etc. are called. The
/// registry throws on collisions (duplicate display name, duplicate cell)
/// so script-side bugs fail at boot, not at the click that hits the wrong NPC.
/// </summary>
public interface INpcRegistry
{
    int NpcCount { get; }
    int FloatingCount { get; }
    int ShopCount { get; }
    int WarpCount { get; }
    int SpawnCount { get; }
    int MapFlagCount { get; }
    int ItemCount { get; }
    int ComboCount { get; }

    void AddNpc(NpcRegistration registration);
    void AddFloatingNpc(FloatingNpcRegistration registration);
    void AddShop(ShopRegistration registration);
    void AddWarp(WarpRegistration registration);
    void AddSpawn(SpawnRegistration registration);
    void AddMapFlag(MapFlagRegistration registration);
    void AddItem(ItemRegistration registration);
    void AddCombo(ComboRegistration registration);

    NpcRegistration? GetNpcByName(string name);
    FloatingNpcRegistration? GetFloatingByName(string name);
    /// <summary>Lookup by numeric item id (rAthena item_db.id).</summary>
    ItemRegistration? GetItemById(int id);
    /// <summary>Lookup by aegis name (rAthena item_db.name_aegis).</summary>
    ItemRegistration? GetItemByAegis(string aegisName);

    IReadOnlyCollection<NpcRegistration> AllNpcs();
    IReadOnlyCollection<FloatingNpcRegistration> AllFloatingNpcs();
    IReadOnlyCollection<ShopRegistration> AllShops();
    IReadOnlyCollection<WarpRegistration> AllWarps();
    IReadOnlyCollection<SpawnRegistration> AllSpawns();
    IReadOnlyCollection<MapFlagRegistration> AllMapFlags();
    IReadOnlyCollection<ItemRegistration> AllItems();
    IReadOnlyCollection<ComboRegistration> AllCombos();

    /// <summary>Drop everything. Used by hot-reload (Phase 2+).</summary>
    void Clear();
}
