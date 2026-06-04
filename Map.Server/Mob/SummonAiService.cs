using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Movement;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mob;

/// <summary>
/// First-slice summon AI — collapses the rAthena per-type AI loops
/// (pet_ai, hom_ai, merc_ai, elem_ai, mob_ai_sub_hard_slavemob) into
/// one ticker keyed by <see cref="Entity.MasterId"/>.
///
/// Per-tick decisions:
/// <list type="number">
///   <item>If the master is gone / off-map → despawn the summon
///         (rAthena <c>pet_unfollow</c>, <c>mercenary_delete</c>).</item>
///   <item>Follow: if Chebyshev distance to master &gt; <see cref="FollowDistance"/>,
///         walk toward master.</item>
///   <item>Assist: if master has an active <see cref="Combat.AttackState"/>
///         and the summon isn't engaged, latch onto the same target via
///         <see cref="IAttackService.StartAttack"/>.</item>
/// </list>
/// </summary>
public sealed class SummonAiService : ISummonAiService
{
    private const int FollowDistance = 3;
    private const int ThinkIntervalMs = 200;

    private readonly IEntityRegistry _entities;
    private readonly IAttackService _attack;
    private readonly IMovementService _movement;
    private readonly IMobLooterService? _looter;
    private readonly ILogger<SummonAiService> _logger;
    private readonly Dictionary<EntityId, long> _lastThink = new();

    public SummonAiService(
        IEntityRegistry entities,
        IAttackService attack,
        IMovementService movement,
        ILogger<SummonAiService> logger,
        IMobLooterService? looter = null)
    {
        _entities = entities;
        _attack = attack;
        _movement = movement;
        _looter = looter;
        _logger = logger;
    }

    public void Tick(long nowTick)
    {
        foreach (var entity in _entities.All().ToArray())
        {
            if (entity.MasterId is not { } masterId) continue;
            if (_lastThink.TryGetValue(entity.Id, out var last) && nowTick - last < ThinkIntervalMs) continue;
            _lastThink[entity.Id] = nowTick;

            var master = _entities.Get(masterId);
            if (master == null || master.MapId != entity.MapId)
            {
                // Master gone or warped away — despawn the summon.
                _logger.LogDebug(
                    "Summon {Summon} lost master {Master}; despawning",
                    entity.Id.Value, masterId.Value);
                _entities.Remove(entity.Id);
                continue;
            }

            // 1. Follow if too far.
            int dist = Math.Max(Math.Abs(entity.X - master.X), Math.Abs(entity.Y - master.Y));
            if (dist > FollowDistance && entity.Walk == null)
            {
                _movement.TryStartWalk(entity, master.X, master.Y);
            }

            // 2. Assist: latch onto master's target if not already engaged.
            if (master.Attack is { } masterAttack && entity.Attack == null)
            {
                _attack.StartAttack(entity, masterAttack.TargetId, continuous: true);
            }

            // 3. Pet loot pickup — rAthena pet_ai_sub_hard loot branch (pet.cpp:1819-1870). Only a
            // looter pet (AutoLootMax > 0, bag not full) that is near its master and not engaged with
            // an enemy hunts floor items: walk onto the item, then pick it into the loot bag.
            if (entity is PetEntity pet && _looter != null && entity.Attack == null
                && pet.AutoLootMax > 0 && pet.LootItems.Count < pet.AutoLootMax
                && dist <= FollowDistance)
            {
                var loot = _looter.FindNearestLoot(pet, IMobLooterService.DefaultLootRange);
                if (loot != null)
                {
                    var lootDist = Math.Max(Math.Abs(loot.X - pet.X), Math.Abs(loot.Y - pet.Y));
                    if (lootDist <= 1)
                    {
                        if (_entities.Get(loot.Id) == loot)
                        {
                            _entities.Remove(loot.Id);
                            pet.LootItems.Add(new MobLootSlot(loot.ItemId, loot.Amount, pet.ClassId));
                            _logger.LogDebug("pet {Pet} looted item {Item} x{Amt} (bag {N}/{Max})",
                                pet.Id.Value, loot.ItemId, loot.Amount, pet.LootItems.Count, pet.AutoLootMax);
                        }
                    }
                    else if (entity.Walk == null)
                    {
                        _movement.TryStartWalk(pet, loot.X, loot.Y);
                    }
                }
            }
        }

        // Periodic prune for despawned entities.
        if (_lastThink.Count > 0 && (nowTick & 0x3FFF) == 0)
        {
            var stale = _lastThink.Keys.Where(id => _entities.Get(id) == null).ToList();
            foreach (var id in stale) _lastThink.Remove(id);
        }
    }
}
