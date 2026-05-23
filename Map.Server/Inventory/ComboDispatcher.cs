using Map.Server.Entities;
using Map.Server.Inventory.Script;
using Map.Server.Items;
using Map.Server.Scripting;
using Map.Server.Scripting.Records;
using Map.Server.Status;
using Microsoft.ClearScript;

namespace Map.Server.Inventory;

/// <summary>
/// Default implementation. Resolves combo member aegis names → numeric
/// ids via <see cref="IItemCatalog"/>, intersects against the equipped
/// id set, and for every match invokes the registered <c>onActive</c>
/// hook through the shared <see cref="ScriptHost"/> engine.
///
/// <para>
/// Resolution caches aegis→id pairs after the first lookup — the catalog
/// snapshot is stable for the server's lifetime, so caching is safe and
/// gets the per-recalc cost down to a hash lookup per member.
/// </para>
///
/// <para>
/// The host context handed to <c>onActive</c> is a <see cref="ScriptedBonusHost"/>
/// (reused from the runtime DSL bridge — same surface). Wrapping in a
/// JS Proxy via <c>__invokeHookWithCtx</c> installed by ScriptHost gives
/// us fail-soft semantics: unknown method names on the host return 0
/// instead of throwing, matching the runtime DSL bridge's behavior so
/// combo scripts that touch rAthena builtins we haven't ported (e.g.
/// <c>getenchantgrade</c>) silently no-op rather than crashing the recalc.
/// </para>
/// </summary>
public sealed class ComboDispatcher : IComboDispatcher
{
    private readonly ScriptHost _scriptHost;
    private readonly INpcRegistry _registry;
    private readonly IItemCatalog _catalog;
    private readonly IPlayerBonusService? _bonusSvc;
    private readonly IEntityRegistry? _entities;
    private readonly ILogger<ComboDispatcher> _logger;

    /// <summary>
    /// Aegis → numeric id cache. <see cref="IItemCatalog"/> already has
    /// <see cref="IItemCatalog.GetByAegisName"/>, but we hit it 17,000+
    /// times per recalc (every member of every combo) so a small cache
    /// trades a few KB for ~25 µs of recalc-time work.
    /// </summary>
    private readonly Dictionary<string, uint?> _aegisIdCache = new(StringComparer.Ordinal);

    public ComboDispatcher(
        ScriptHost scriptHost,
        INpcRegistry registry,
        IItemCatalog catalog,
        ILogger<ComboDispatcher> logger,
        IPlayerBonusService? bonusSvc = null,
        IEntityRegistry? entities = null)
    {
        _scriptHost = scriptHost;
        _registry = registry;
        _catalog = catalog;
        _bonusSvc = bonusSvc;
        _entities = entities;
        _logger = logger;
    }

    public void ApplyActiveCombos(
        IReadOnlyList<InventoryItem> equipped,
        EquipBonusBundle bundle,
        PlayerEntity player)
    {
        if (_registry.ComboCount == 0) return;

        // Build the equipped-NameId set once per recalc. EquipBonusAggregator
        // walks the same list a few lines earlier; if recalc gets hot we can
        // hoist the set up the call chain.
        var equippedIds = new HashSet<uint>();
        foreach (var item in equipped)
            if (item.Equip != 0) equippedIds.Add(item.NameId);
        if (equippedIds.Count == 0) return;

        var invoker = _scriptHost.Engine.Script.__invokeHookWithCtx;

        foreach (var combo in _registry.AllCombos())
        {
            if (combo.Hooks.OnActive is not { } handle) continue;
            if (!AllMembersEquipped(combo.Members, equippedIds)) continue;

            // Each combo gets its own host instance so per-combo state
            // (host caches, ambient args) doesn't bleed. The host writes
            // into the shared bundle in-place.
            var host = new ScriptedBonusHost(player, bundle, equipped, _catalog, _bonusSvc, _entities);
            try
            {
                invoker(handle.Value, host);
            }
            catch (ScriptEngineException ex)
            {
                // A combo throwing shouldn't kill the recalc — other combos
                // and per-item bonuses still need to land. Log + continue.
                _logger.LogDebug(ex,
                    "Combo {ComboId} onActive threw for {Player}: {Details}",
                    combo.ComboId, player.Name, ex.ErrorDetails);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex,
                    "Combo {ComboId} onActive threw for {Player}",
                    combo.ComboId, player.Name);
            }
        }
    }

    private bool AllMembersEquipped(IReadOnlyList<string> members, HashSet<uint> equippedIds)
    {
        // Short-circuit: any unresolved or missing member → combo doesn't fire.
        for (var i = 0; i < members.Count; i++)
        {
            var id = ResolveAegis(members[i]);
            if (id == null || !equippedIds.Contains(id.Value)) return false;
        }
        return true;
    }

    private uint? ResolveAegis(string aegis)
    {
        if (_aegisIdCache.TryGetValue(aegis, out var cached)) return cached;
        var row = _catalog.GetByAegisName(aegis);
        var id = row?.Id;
        _aegisIdCache[aegis] = id;
        return id;
    }
}
