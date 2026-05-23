using Map.Server.Scripting.Records;

namespace Map.Server.Scripting;

/// <summary>
/// Default implementation. Single-threaded by contract: <see cref="ScriptHost"/>
/// drives all writes from one boot-time pass, and reads happen from the game
/// loop thread.
/// </summary>
public sealed class NpcRegistry : INpcRegistry
{
    private readonly Dictionary<string, NpcRegistration> _npcsByName = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Map, short X, short Y), NpcRegistration> _npcsByCell = new();
    private readonly Dictionary<string, FloatingNpcRegistration> _floatingByName = new(StringComparer.Ordinal);
    private readonly List<ShopRegistration> _shops = new();
    private readonly List<WarpRegistration> _warps = new();
    private readonly List<SpawnRegistration> _spawns = new();
    private readonly List<MapFlagRegistration> _mapFlags = new();
    // Items indexed by numeric id (item_db.id). Aegis-name lookup goes
    // through IItemCatalog (the SQL surface owns aegis→id); the
    // registrar is purely id-keyed hook attachment.
    private readonly Dictionary<int, ItemRegistration> _itemsById = new();
    private readonly List<ComboRegistration> _combos = new();

    public int NpcCount => _npcsByName.Count;
    public int FloatingCount => _floatingByName.Count;
    public int ShopCount => _shops.Count;
    public int WarpCount => _warps.Count;
    public int SpawnCount => _spawns.Count;
    public int MapFlagCount => _mapFlags.Count;
    public int ItemCount => _itemsById.Count;
    public int ComboCount => _combos.Count;

    public void AddNpc(NpcRegistration registration)
    {
        if (_npcsByName.ContainsKey(registration.Name))
        {
            throw new ScriptRegistrationException(
                $"Duplicate NPC name '{registration.Name}'. " +
                "Each registerNpc()/registerFloatingNpc() must use a globally unique name.");
        }
        // Floating-name collision check too — names must be unique across both spaces
        // so Phase 5's doevent("Name::OnFoo") dispatch is unambiguous.
        if (_floatingByName.ContainsKey(registration.Name))
        {
            throw new ScriptRegistrationException(
                $"NPC name '{registration.Name}' collides with a registered floating NPC.");
        }
        var cell = (registration.Map, registration.X, registration.Y);
        if (_npcsByCell.TryGetValue(cell, out var existing))
        {
            throw new ScriptRegistrationException(
                $"Two NPCs claim the same cell {registration.Map} ({registration.X},{registration.Y}): " +
                $"'{existing.Name}' and '{registration.Name}'. Only one NPC may occupy a cell.");
        }
        _npcsByName.Add(registration.Name, registration);
        _npcsByCell.Add(cell, registration);
    }

    public void AddFloatingNpc(FloatingNpcRegistration registration)
    {
        if (_floatingByName.ContainsKey(registration.Name))
        {
            throw new ScriptRegistrationException(
                $"Duplicate floating NPC name '{registration.Name}'.");
        }
        if (_npcsByName.ContainsKey(registration.Name))
        {
            throw new ScriptRegistrationException(
                $"Floating NPC name '{registration.Name}' collides with a registered scripted NPC.");
        }
        _floatingByName.Add(registration.Name, registration);
    }

    public void AddShop(ShopRegistration registration) => _shops.Add(registration);
    public void AddWarp(WarpRegistration registration) => _warps.Add(registration);
    public void AddSpawn(SpawnRegistration registration) => _spawns.Add(registration);
    public void AddMapFlag(MapFlagRegistration registration) => _mapFlags.Add(registration);

    public void AddItem(ItemRegistration registration)
    {
        if (_itemsById.ContainsKey(registration.Id))
        {
            throw new ScriptRegistrationException(
                $"Duplicate registerItem() for id {registration.Id}. " +
                "Each item id may only be registered once across the whole scripts bundle.");
        }
        _itemsById.Add(registration.Id, registration);
    }

    public void AddCombo(ComboRegistration registration) => _combos.Add(registration);

    public NpcRegistration? GetNpcByName(string name) =>
        _npcsByName.GetValueOrDefault(name);

    public FloatingNpcRegistration? GetFloatingByName(string name) =>
        _floatingByName.GetValueOrDefault(name);

    public ItemRegistration? GetItemById(int id) =>
        _itemsById.GetValueOrDefault(id);

    public IReadOnlyCollection<NpcRegistration> AllNpcs() => _npcsByName.Values;
    public IReadOnlyCollection<FloatingNpcRegistration> AllFloatingNpcs() => _floatingByName.Values;
    public IReadOnlyCollection<ShopRegistration> AllShops() => _shops;
    public IReadOnlyCollection<WarpRegistration> AllWarps() => _warps;
    public IReadOnlyCollection<SpawnRegistration> AllSpawns() => _spawns;
    public IReadOnlyCollection<MapFlagRegistration> AllMapFlags() => _mapFlags;
    public IReadOnlyCollection<ItemRegistration> AllItems() => _itemsById.Values;
    public IReadOnlyCollection<ComboRegistration> AllCombos() => _combos;

    public void Clear()
    {
        _npcsByName.Clear();
        _npcsByCell.Clear();
        _floatingByName.Clear();
        _shops.Clear();
        _warps.Clear();
        _spawns.Clear();
        _mapFlags.Clear();
        _itemsById.Clear();
        _combos.Clear();
    }
}

/// <summary>
/// Thrown when a TS-side <c>register*()</c> call violates a runtime invariant
/// (duplicate name, missing required field, non-callable hook value, etc.).
/// Surfaces with the script source location so authors can find the bad call
/// fast.
/// </summary>
public sealed class ScriptRegistrationException : Exception
{
    public ScriptRegistrationException(string message) : base(message) { }
    public ScriptRegistrationException(string message, Exception inner) : base(message, inner) { }
}
