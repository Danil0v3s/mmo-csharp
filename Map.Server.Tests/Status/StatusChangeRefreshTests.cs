using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Status;

/// <summary>
/// ST.7 — verifies IStatusChangeService.Refresh re-applies the
/// weapon-element SC family on weapon-swap; leaves non-weapon SCs
/// alone; no-ops when no weapon SCs are active.
/// </summary>
public class StatusChangeRefreshTests
{
    [Fact]
    public void Refresh_ReappliesActiveFireweapon()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);

        ctx.Service.Start(pc, StatusType.Fireweapon, val1: 3, 0, 0, 0, durationMs: 30_000);
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Fireweapon));

        var refreshed = ctx.Service.Refresh(pc);

        Assert.Equal(1, refreshed);
        // SC still present after refresh (re-applied with same val1).
        var sc = ctx.Service.Get(pc, StatusType.Fireweapon);
        Assert.NotNull(sc);
        Assert.Equal(3, sc.Val1);
    }

    [Fact]
    public void Refresh_LeavesNonWeaponScAlone()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(2, 100, 100);
        ctx.Service.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 30_000);
        ctx.Service.Start(pc, StatusType.Poison, 0, 0, 0, 0, 30_000);

        // No weapon-element SCs active → Refresh = 0.
        var refreshed = ctx.Service.Refresh(pc);
        Assert.Equal(0, refreshed);
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Blessing));
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Poison));
    }

    [Fact]
    public void Refresh_RefreshesAllFourElementVariants()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(3, 100, 100);
        ctx.Service.Start(pc, StatusType.Fireweapon, 3, 0, 0, 0, 30_000);
        ctx.Service.Start(pc, StatusType.Earthweapon, 3, 0, 0, 0, 30_000);
        ctx.Service.Start(pc, StatusType.Windweapon, 3, 0, 0, 0, 30_000);
        ctx.Service.Start(pc, StatusType.Waterweapon, 3, 0, 0, 0, 30_000);

        var refreshed = ctx.Service.Refresh(pc);
        Assert.Equal(4, refreshed);
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Fireweapon));
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Earthweapon));
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Windweapon));
        Assert.NotNull(ctx.Service.Get(pc, StatusType.Waterweapon));
    }

    [Fact]
    public void Refresh_NoActiveSc_ReturnsZero()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(4, 100, 100);
        Assert.Equal(0, ctx.Service.Refresh(pc));
    }

    private static TestContext Build()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(),
            NullLogger<MovementService>.Instance);
        var mobDb = new StubMobDb();
        var spawnRegistry = new MobSpawnRegistry();
        var ids = new EntityIdAllocator();
        var itemCatalog = new EmptyItemCatalog();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            spawnRegistry, entities, world, mobDb, itemCatalog, itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var service = new StatusChangeService(damage, entities, new StatusEffectRegistry(),
            NullLogger<StatusChangeService>.Instance);
        return new TestContext(service, entities, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        StatusChangeService Service,
        EntityRegistry Entities,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }
    }

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string n) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }

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

    private sealed class EmptyItemCatalog : IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint id) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }
}
