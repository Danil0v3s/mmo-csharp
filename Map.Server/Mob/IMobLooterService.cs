using Map.Server.Entities;

namespace Map.Server.Mob;

/// <summary>
/// Port of rAthena's MD_LOOTER scan in <c>mob_ai_sub_hard</c>
/// (mob.cpp:2008-2129). When a mob has the <see cref="Status.MobMode.Looter"/>
/// bit set and isn't engaged in combat, the AI looks for nearby
/// floor items inside <c>battle_config.loot_range</c> (default 8),
/// walks to the closest one, and on arrival appends it to its
/// per-mob loot bag (<see cref="MobEntity.LootItems"/>).
///
/// <para>Drop-back-on-death (mob_dead at mob.cpp:3244-3260)
/// re-spawns the bag contents as floor items at the mob's cell —
/// that's owned by the existing mob-death path and orthogonal to
/// this service.</para>
/// </summary>
public interface IMobLooterService
{
    /// <summary>rAthena <c>battle_config.loot_range</c> default.</summary>
    public const short DefaultLootRange = 8;

    /// <summary>
    /// True iff the mob qualifies for a looter scan this tick:
    /// MD_LOOTER bit set, bag not yet full
    /// (<see cref="MobLootSlot.LootBagSize"/>), and the mob can act
    /// (handler does its own canact_tick check upstream).
    /// </summary>
    bool IsLootEligible(MobEntity mob);

    /// <summary>
    /// rAthena <c>mob_ai_sub_hard_lootsearch</c> wrapper —
    /// returns the nearest live floor item within
    /// <paramref name="range"/> cells of the mob, or null if none.
    /// </summary>
    FloorItemEntity? FindNearestLoot(MobEntity mob, short range);

    /// <summary>
    /// Move <paramref name="item"/> from the floor onto
    /// <paramref name="mob"/>'s loot bag. Removes the entity from
    /// the registry, appends to <see cref="MobEntity.LootItems"/>
    /// (drops the oldest slot if the bag is full, mirroring
    /// rAthena mob.cpp:2119). Returns false if the bag transfer
    /// failed (item already gone, registry mismatch).
    /// </summary>
    bool Collect(MobEntity mob, FloorItemEntity item);
}
