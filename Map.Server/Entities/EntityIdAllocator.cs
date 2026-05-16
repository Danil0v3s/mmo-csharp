namespace Map.Server.Entities;

/// <summary>
/// Allocates unique <see cref="EntityId"/>s for non-PC entities (mobs, NPCs,
/// floor items, skill units). PCs use their character_id directly; this
/// allocator never hands out an id in the PC range.
///
/// Ranges mirror rAthena conventions (map.hpp):
///   - START_NPC_ID  = 800_000_000  → NPCs
///   - MIN_FLOORITEM = 2_000_000_000 → floor items
///   - mobs/skill units occupy 400_000_000–799_999_999 (between PCs and NPCs).
///
/// Thread-safe via <see cref="Interlocked"/>; mutation happens from both the
/// game tick (mob spawn) and gRPC handlers (npc reload, item drop forwards).
/// </summary>
public sealed class EntityIdAllocator
{
    private int _nextMob = 400_000_000;
    private int _nextNpc = 800_000_000;
    private int _nextItem = unchecked((int)2_000_000_000);
    private int _nextSkill = 1_500_000_000;

    public EntityId NextMob() => new(Interlocked.Increment(ref _nextMob));
    public EntityId NextNpc() => new(Interlocked.Increment(ref _nextNpc));
    public EntityId NextItem() => new(Interlocked.Increment(ref _nextItem));
    public EntityId NextSkill() => new(Interlocked.Increment(ref _nextSkill));
}
