using Map.Server.Entities;
using Map.Server.World;

namespace Map.Server.Items;

/// <summary>
/// rAthena <c>db/map_drops.yml</c> consumer. Rolls map-level drop
/// overrides AFTER a mob's own drop table. Two flavors:
/// <list type="bullet">
/// <item><b>GlobalDrops</b> (MobFilterAegis null) — apply to every
/// mob on the map.</item>
/// <item><b>Per-mob overrides</b> (MobFilterAegis set) — apply only
/// when the dead mob's aegis matches.</item>
/// </list>
/// <para>rAthena semantics: Rate is per 100000, and map-drop rolls
/// <i>bypass</i> the global drop-rate multipliers (battle_config
/// item_rate_common etc.). Each surviving entry spawns a floor item
/// at the mob's death cell.</para>
/// </summary>
public interface IMapDropService
{
    /// <summary>
    /// Iterate the map's drop list. For each entry whose filter
    /// matches (null = global) and whose Rate roll succeeds, spawn a
    /// FloorItemEntity at <paramref name="x"/>/<paramref name="y"/>
    /// keyed to <paramref name="ownerCharId"/> for the standard
    /// pickup-protection window. Returns the number of items dropped.
    /// </summary>
    int RollAndDrop(MapData map, MobEntity mob, PlayerEntity? lastHitter, short x, short y);

    /// <summary>True iff at least one map_drop_db row was loaded.</summary>
    bool HasData { get; }
}
