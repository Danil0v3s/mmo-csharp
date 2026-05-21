using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mob;

/// <summary>
/// Default <see cref="IMobLooterService"/> impl. Walks the entity
/// registry for nearby floor items. Single-pass nearest-neighbour
/// (Chebyshev), which matches rAthena's
/// <c>mob_ai_sub_hard_lootsearch</c> sort behavior closely enough
/// — the C++ side also picks distance-ordered.
/// </summary>
public sealed class MobLooterService : IMobLooterService
{
    private readonly IEntityRegistry _entities;
    private readonly ILogger<MobLooterService> _logger;

    public MobLooterService(IEntityRegistry entities, ILogger<MobLooterService> logger)
    {
        _entities = entities;
        _logger = logger;
    }

    public bool IsLootEligible(MobEntity mob)
    {
        if (!mob.IsLooter) return false;
        // rAthena gate at mob.cpp:2010 — battle_config.monster_loot_type
        // 0 means "stop looting at cap." We model the cap directly here.
        if (mob.LootItems.Count >= MobLootSlot.LootBagSize) return false;
        return true;
    }

    public FloorItemEntity? FindNearestLoot(MobEntity mob, short range)
    {
        var inRange = _entities.ForEachInRange(
            mob.MapId, mob.X, mob.Y, range, EntityType.Item);
        if (inRange.Count == 0) return null;

        FloorItemEntity? closest = null;
        int closestDist = int.MaxValue;
        foreach (var e in inRange)
        {
            if (e is not FloorItemEntity item) continue;
            var dist = Math.Max(Math.Abs(item.X - mob.X), Math.Abs(item.Y - mob.Y));
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = item;
            }
        }
        return closest;
    }

    public bool Collect(MobEntity mob, FloorItemEntity item)
    {
        // Validate the item still exists in the registry — handlers may
        // race with player pickups / despawn sweeps.
        if (_entities.Get(item.Id) != item) return false;
        _entities.Remove(item.Id);

        // rAthena mob.cpp:2114-2123 — append until cap, then evict the
        // oldest entry (FIFO) and keep the new item at the tail. We
        // never *grow* beyond LootBagSize.
        if (mob.LootItems.Count >= MobLootSlot.LootBagSize)
            mob.LootItems.RemoveAt(0);

        mob.LootItems.Add(new MobLootSlot(item.ItemId, item.Amount, mob.ClassId));
        _logger.LogDebug(
            "mob {Mob} looted item {Item} x{Amount} (bag {Count}/{Cap})",
            mob.Id, item.ItemId, item.Amount, mob.LootItems.Count, MobLootSlot.LootBagSize);
        return true;
    }
}
