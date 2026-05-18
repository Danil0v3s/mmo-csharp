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

    void AddNpc(NpcRegistration registration);
    void AddFloatingNpc(FloatingNpcRegistration registration);
    void AddShop(ShopRegistration registration);

    NpcRegistration? GetNpcByName(string name);
    FloatingNpcRegistration? GetFloatingByName(string name);

    IReadOnlyCollection<NpcRegistration> AllNpcs();
    IReadOnlyCollection<FloatingNpcRegistration> AllFloatingNpcs();
    IReadOnlyCollection<ShopRegistration> AllShops();

    /// <summary>Drop everything. Used by hot-reload (Phase 2+).</summary>
    void Clear();
}
