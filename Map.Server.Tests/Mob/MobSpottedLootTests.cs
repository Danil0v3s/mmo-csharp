using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.9c — tests for the spotted-log helpers (mob.cpp:99-145),
/// the warp-chase entry point (mob.cpp:1776), and the MD_LOOTER
/// pickup service (mob.cpp:2008-2129).
/// </summary>
public class MobSpottedLootTests
{
    // ---- spotted log ----

    [Fact]
    public void Spotted_Add_GrowsLogAndFlipsIsSpotted()
    {
        var mob = MakeMob(0, 0);
        Assert.False(mob.IsSpotted);

        MobSpotted.Add(mob, charId: 42);
        Assert.True(mob.IsSpotted);
        Assert.Single(mob.SpottedLog);

        // Adding the same id twice is a no-op (HashSet semantics);
        // rAthena's loop short-circuits on the first match.
        MobSpotted.Add(mob, charId: 42);
        Assert.Single(mob.SpottedLog);
    }

    [Fact]
    public void Spotted_Add_RespectsDamageLogSizeCap()
    {
        var mob = MakeMob(0, 0);
        // rAthena DAMAGELOG_SIZE = 30. Adding more than that drops the
        // overflow (Aegis behaviour: first-N wins).
        for (int i = 1; i <= MobSpotted.MaxSpotted + 5; i++) MobSpotted.Add(mob, i);
        Assert.Equal(MobSpotted.MaxSpotted, mob.SpottedLog.Count);
    }

    [Fact]
    public void Spotted_Clean_EvictsDisconnectedPlayers()
    {
        var mob = MakeMob(0, 0);
        MobSpotted.Add(mob, 100);
        MobSpotted.Add(mob, 200);
        Assert.Equal(2, mob.SpottedLog.Count);

        var registry = new FakeRegistry();
        // 100 is still online; 200 has logged out.
        registry.AddPc(charId: 100, x: 1, y: 1);

        MobSpotted.Clean(mob, registry);

        Assert.Contains(100, mob.SpottedLog);
        Assert.DoesNotContain(200, mob.SpottedLog);
    }

    // ---- warp chase ----

    [Fact]
    public void WarpChase_SameMap_ReturnsNotApplicable()
    {
        // rAthena mob.cpp:1796 — when target is on the same map the
        // warp-chase short-circuits. Our impl applies the same gate.
        var mob = MakeMob(0, 0);
        var target = MakeMob(10, 10, id: 99);

        var svc = new MobWarpChaseService(
            new FakeRegistry(), NullLogger<MobWarpChaseService>.Instance);
        Assert.Equal(WarpChaseResult.NotApplicable, svc.TryWarpChase(mob, target));
    }

    [Fact]
    public void WarpChase_CrossMap_NoWarpRegistered_ReturnsNotApplicable()
    {
        // Data-pending: with no warp NPCs in the registry we still
        // return NotApplicable. The canonical surface is what we're
        // shipping; the scan body lands once NpcEntity gains the
        // warp subtype.
        var mob = MakeMob(0, 0);
        var target = MakeMob(10, 10, id: 99);
        // Force the target onto a different map via reflection-free path:
        // the helper accepts mapId in its ctor, so build a fresh one.
        var farTarget = MakeMobOnMap(10, 10, id: 99, mapId: 99);

        var svc = new MobWarpChaseService(
            new FakeRegistry(), NullLogger<MobWarpChaseService>.Instance);
        Assert.Equal(WarpChaseResult.NotApplicable, svc.TryWarpChase(mob, farTarget));
    }

    // ---- looter ----

    [Fact]
    public void Looter_IsLootEligible_OnlyWhenLooterBitSetAndBagHasRoom()
    {
        var svc = new MobLooterService(
            new FakeRegistry(), NullLogger<MobLooterService>.Instance);

        var mob = MakeMob(0, 0);
        // Default mob = no Looter bit.
        Assert.False(svc.IsLootEligible(mob));

        mob.Stats.Mode |= MobMode.Looter;
        Assert.True(svc.IsLootEligible(mob));

        // Fill the bag — eligibility flips to false.
        for (int i = 0; i < MobLootSlot.LootBagSize; i++)
            mob.LootItems.Add(new MobLootSlot(501, 1, mob.ClassId));
        Assert.False(svc.IsLootEligible(mob));
    }

    [Fact]
    public void Looter_FindNearestLoot_PicksClosestInRange()
    {
        var registry = new FakeRegistry();
        var mob = MakeMob(10, 10);
        mob.Stats.Mode |= MobMode.Looter;
        registry.Add(mob);

        // Drop 3 items at varying distances; only two are in default
        // loot range (8 cells); pick the closest.
        var near = MakeFloorItem(id: 1001, itemId: 501, x: 11, y: 11);    // dist 1
        var mid = MakeFloorItem(id: 1002, itemId: 502, x: 13, y: 13);     // dist 3
        var far = MakeFloorItem(id: 1003, itemId: 503, x: 30, y: 30);     // dist 20 (out)
        registry.Add(near);
        registry.Add(mid);
        registry.Add(far);

        var svc = new MobLooterService(registry, NullLogger<MobLooterService>.Instance);
        var pick = svc.FindNearestLoot(mob, IMobLooterService.DefaultLootRange);
        Assert.NotNull(pick);
        Assert.Equal(near.Id, pick!.Id);
    }

    [Fact]
    public void Looter_Collect_TransfersAndEvictsOldestAtCap()
    {
        var registry = new FakeRegistry();
        var mob = MakeMob(0, 0);
        mob.Stats.Mode |= MobMode.Looter;
        registry.Add(mob);

        // Fill bag with 10 markers (501..510).
        for (int i = 0; i < MobLootSlot.LootBagSize; i++)
            mob.LootItems.Add(new MobLootSlot(500 + i, 1, mob.ClassId));

        // Drop an 11th item and collect it — bag stays at cap, oldest
        // slot (500) is evicted, new item joins the tail.
        var fresh = MakeFloorItem(id: 1010, itemId: 600, x: 0, y: 0);
        registry.Add(fresh);

        var svc = new MobLooterService(registry, NullLogger<MobLooterService>.Instance);
        Assert.True(svc.Collect(mob, fresh));

        Assert.Equal(MobLootSlot.LootBagSize, mob.LootItems.Count);
        Assert.Equal(600, mob.LootItems[^1].ItemId);
        Assert.DoesNotContain(mob.LootItems, s => s.ItemId == 500);
        // Floor item removed from the registry.
        Assert.Null(registry.Get(fresh.Id));
    }

    // ---- helpers ----

    private static MobEntity MakeMob(short x, short y, int id = 1) =>
        MakeMobOnMap(x, y, id, mapId: 1);

    private static MobEntity MakeMobOnMap(short x, short y, int id, uint mapId)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 100 };
        var origin = new MobSpawnEntry { MapId = mapId, MobClassId = 1002 };
        var mob = new MobEntity(new EntityId(id), db, origin, mapId: mapId, x: x, y: y);
        mob.MaxHp = 100;
        mob.Hp = 100;
        return mob;
    }

    private static FloorItemEntity MakeFloorItem(int id, int itemId, short x, short y)
        => new(new EntityId(id), itemId, amount: 1, mapId: 1, x: x, y: y,
            subX: 0, subY: 0, droppedAtTick: 0);

    /// <summary>
    /// Tiny IEntityRegistry for these tests. Lighter than the full
    /// EntityRegistry (no spatial index, no IMapWorldRegistry); the
    /// services we test only exercise ForEachInRange / Get / Remove /
    /// All / Add.
    /// </summary>
    private sealed class FakeRegistry : IEntityRegistry
    {
        private readonly Dictionary<EntityId, Entity> _byId = new();
        private int _nextPcId = 10000;

        public int Count => _byId.Count;
        public void Add(Entity e) => _byId[e.Id] = e;
        public Entity? Remove(EntityId id) { _byId.Remove(id, out var e); return e; }
        public Entity? Get(EntityId id) => _byId.GetValueOrDefault(id);
        public bool Contains(EntityId id) => _byId.ContainsKey(id);
        public IEnumerable<Entity> All() => _byId.Values;
        public void Move(EntityId id, short newX, short newY) { /* no-op */ }
        public IReadOnlyList<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask)
            => Array.Empty<Entity>();

        public IReadOnlyList<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask)
        {
            var result = new List<Entity>();
            foreach (var e in _byId.Values)
            {
                if (e.MapId != mapId) continue;
                if ((e.Type & mask) == 0) continue;
                if (Math.Abs(e.X - cx) > range || Math.Abs(e.Y - cy) > range) continue;
                result.Add(e);
            }
            return result;
        }

        public void AddPc(int charId, short x, short y)
        {
            // PlayerEntity ctors are heavy; for spotted-log tests we
            // only need an entity with Type=Pc, the right CharacterId,
            // and Hp > 0. CharacterId derives from Id.Value, so use
            // that id as the char id directly.
            var p = TestPlayerEntity.Create(charId, mapId: 1, x: x, y: y);
            _byId[p.Id] = p;
            _nextPcId++;
        }
    }

    /// <summary>
    /// Bridge to PlayerEntity's ctor without hauling in the whole
    /// session / repository graph. PlayerEntity.CharacterId == Id.Value,
    /// which is enough for MobSpotted.Clean to match.
    /// </summary>
    private static class TestPlayerEntity
    {
        public static PlayerEntity Create(int charId, uint mapId, short x, short y)
        {
            var p = new PlayerEntity(
                characterId: charId,
                accountId: 1,
                name: $"PC{charId}",
                sessionId: Guid.NewGuid(),
                mapId: mapId,
                x: x,
                y: y);
            p.MaxHp = 100;
            p.Hp = 100;
            return p;
        }
    }
}
