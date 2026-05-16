using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Spawn;
using Map.Server.Tests.Visibility;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Spawn;

public class MobSpawnServiceTests
{
    private const short MapSize = 60;

    [Fact]
    public void SpawnInitial_PopulatesAmountForEachEntry()
    {
        var ctx = NewContext();
        ctx.SpawnRegistry.Add(NewEntry(ctx, classId: 1002, x: 30, y: 30, xs: 5, ys: 5, amount: 5));
        ctx.SpawnRegistry.Add(NewEntry(ctx, classId: 1004, x: 10, y: 10, xs: 2, ys: 2, amount: 3));

        ctx.Service.SpawnInitial();

        var mobs = ctx.Entities.All().OfType<MobEntity>().ToList();
        Assert.Equal(8, mobs.Count);
        Assert.Equal(5, mobs.Count(m => m.ClassId == 1002));
        Assert.Equal(3, mobs.Count(m => m.ClassId == 1004));
    }

    [Fact]
    public void SpawnInitial_PlacesMobsWithinDeclaredBox()
    {
        var ctx = NewContext();
        ctx.SpawnRegistry.Add(NewEntry(ctx, classId: 1002, x: 40, y: 40, xs: 3, ys: 3, amount: 30));

        ctx.Service.SpawnInitial();

        var mobs = ctx.Entities.All().OfType<MobEntity>();
        foreach (var m in mobs)
        {
            Assert.InRange(m.X, 37, 43);
            Assert.InRange(m.Y, 37, 43);
        }
    }

    [Fact]
    public void SpawnInitial_BroadcastsStandEntryForEachNewMob()
    {
        var ctx = NewContext();
        // Viewer must be on the map so visibility has someone to notify.
        var viewer = new PlayerEntity(99, 990, "Viewer", Guid.NewGuid(), ctx.MapId, 30, 30);
        ctx.Entities.Add(viewer);
        ctx.SpawnRegistry.Add(NewEntry(ctx, classId: 1002, x: 30, y: 30, xs: 5, ys: 5, amount: 3));

        ctx.Service.SpawnInitial();

        var standEntries = ctx.Dispatcher.Sent
            .Where(s => s.packet is ZC_NOTIFY_STANDENTRY)
            .ToList();
        Assert.Equal(3, standEntries.Count);
    }

    [Fact]
    public void SpawnInitial_UnknownMobClass_IsSkipped()
    {
        var ctx = NewContext();
        ctx.SpawnRegistry.Add(NewEntry(ctx, classId: 99999, x: 30, y: 30, xs: 5, ys: 5, amount: 5));

        ctx.Service.SpawnInitial();

        Assert.Empty(ctx.Entities.All().OfType<MobEntity>());
    }

    [Fact]
    public void KillMob_RemovesEntityBroadcastsVanishAndSchedulesRespawn()
    {
        var ctx = NewContext();
        // Viewer in range so the vanish broadcast has a recipient.
        var viewer = new PlayerEntity(99, 990, "Viewer", Guid.NewGuid(), ctx.MapId, 30, 30);
        ctx.Entities.Add(viewer);
        ctx.SpawnRegistry.Add(NewEntry(ctx, classId: 1002, x: 30, y: 30, xs: 0, ys: 0, amount: 1));
        ctx.Service.SpawnInitial();
        var mob = ctx.Entities.All().OfType<MobEntity>().Single();

        Assert.True(ctx.Service.KillMob(mob.Id));

        Assert.Null(ctx.Entities.Get(mob.Id));
        Assert.Equal(1, ctx.Service.PendingRespawnCount);
        var vanish = (ZC_NOTIFY_VANISH)ctx.Dispatcher.Sent
            .Last(s => s.packet is ZC_NOTIFY_VANISH).packet;
        Assert.Equal(VanishReason.Died, vanish.Reason);
        Assert.Equal(mob.Id.Value, vanish.EntityId);
    }

    [Fact]
    public void KillMob_NonMobId_ReturnsFalse()
    {
        var ctx = NewContext();
        Assert.False(ctx.Service.KillMob(new EntityId(1500001)));
    }

    [Fact]
    public void Tick_AfterRespawnDelay_BringsMobBack()
    {
        var entry = new MobSpawnEntry
        {
            MobClassId = 1002,
            MapId = 0, // set below once context exists
            X = 30, Y = 30,
            Xs = 0, Ys = 0,
            Amount = 1,
            RespawnDelayMs = 0,
            RespawnJitterMs = 0,
        };
        var ctx = NewContext();
        entry = entry with { MapId = ctx.MapId };
        ctx.SpawnRegistry.Add(entry);
        ctx.Service.SpawnInitial();
        var original = ctx.Entities.All().OfType<MobEntity>().Single();

        ctx.Service.KillMob(original.Id);
        Assert.Empty(ctx.Entities.All().OfType<MobEntity>());

        // Zero-delay respawn means the next Tick re-spawns.
        ctx.Service.Tick();

        var respawned = ctx.Entities.All().OfType<MobEntity>().SingleOrDefault();
        Assert.NotNull(respawned);
        Assert.NotEqual(original.Id, respawned!.Id);
        Assert.Equal(0, ctx.Service.PendingRespawnCount);
    }

    [Fact]
    public void Tick_IdleMobWithElapsedCooldown_StartsWalking()
    {
        var ctx = NewContext(seed: 99);
        ctx.SpawnRegistry.Add(NewEntry(ctx, classId: 1002, x: 30, y: 30, xs: 0, ys: 0, amount: 1));
        ctx.Service.SpawnInitial();
        var mob = ctx.Entities.All().OfType<MobEntity>().Single();

        // Force the wander cooldown to be in the past so Tick picks a target.
        mob.NextWanderTick = 0;
        Assert.Null(mob.Walk);

        ctx.Service.Tick();

        Assert.NotNull(mob.Walk);
    }

    // ---- helpers ----

    private static MobSpawnEntry NewEntry(
        TestContext ctx, int classId, short x, short y, short xs, short ys, int amount) =>
        new()
        {
            MobClassId = classId,
            MapId = ctx.MapId,
            X = x, Y = y, Xs = xs, Ys = ys,
            Amount = amount,
            RespawnDelayMs = 5000,
            RespawnJitterMs = 0,
        };

    private static TestContext NewContext(int seed = 42)
    {
        const string mapName = "test_map";
        var cells = new byte[MapSize * MapSize];
        Array.Fill(cells, (byte)0); // gat 0 = walkable
        var map = new MapData(mapName, MapSize, MapSize, cells);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility, NullLogger<MovementService>.Instance);
        var spawnRegistry = new MobSpawnRegistry();
        var idAlloc = new EntityIdAllocator();
        var mobDb = new StubMobDb(new[]
        {
            new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 55, WalkSpeed = 400 },
            new MobDbEntry { Id = 1004, AegisName = "HORNET", Name = "Hornet", Hp = 169, WalkSpeed = 150 },
        });
        var service = new MobSpawnService(
            spawnRegistry,
            entities,
            world,
            mobDb,
            movement,
            visibility,
            idAlloc,
            NullLogger<MobSpawnService>.Instance,
            new Random(seed));
        return new TestContext(service, spawnRegistry, entities, dispatcher, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        MobSpawnService Service,
        MobSpawnRegistry SpawnRegistry,
        EntityRegistry Entities,
        RecordingDispatcher Dispatcher,
        uint MapId);

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }

    private sealed class StubMobDb : IMobDb
    {
        private readonly Dictionary<int, MobDbEntry> _byId;
        private readonly Dictionary<string, MobDbEntry> _byName;

        public StubMobDb(IEnumerable<MobDbEntry> entries)
        {
            _byId = entries.ToDictionary(e => e.Id);
            _byName = _byId.Values.ToDictionary(e => e.AegisName, StringComparer.OrdinalIgnoreCase);
        }

        public int Count => _byId.Count;
        public MobDbEntry? Get(int classId) => _byId.GetValueOrDefault(classId);
        public MobDbEntry? GetByAegisName(string aegisName) =>
            _byName.GetValueOrDefault(aegisName);
        public IEnumerable<MobDbEntry> All() => _byId.Values;
        public void Reload() { }
    }
}
