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

public class StatusChangeServiceTests
{
    [Fact]
    public void Blessing_AppliesAndReverts_StrIntDexBoost()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.Str = 10;
        pc.Stats.IntStat = 10;
        pc.Stats.Dex = 10;

        ctx.Service.Start(pc, StatusType.Blessing, val1: 5, 0, 0, 0, durationMs: 10_000);

        Assert.Equal(15, pc.Stats.Str);
        Assert.Equal(15, pc.Stats.IntStat);
        Assert.Equal(15, pc.Stats.Dex);

        Assert.True(ctx.Service.End(pc, StatusType.Blessing));

        Assert.Equal(10, pc.Stats.Str);
        Assert.Equal(10, pc.Stats.IntStat);
        Assert.Equal(10, pc.Stats.Dex);
    }

    [Fact]
    public void Poison_TicksDamage_EveryPeriod()
    {
        var ctx = Build();
        var mob = ctx.AddMob(101, 100);
        mob.MaxHp = 1000;
        mob.Hp = 1000;
        // Period is 1500ms, damage = max(1, maxhp * 1.5%) = 15 per tick.

        ctx.Service.Start(mob, StatusType.Poison, 0, 0, 0, 0, durationMs: 10_000, nowTick: 0);

        ctx.Service.Tick(nowTick: 100);
        Assert.Equal(1000, mob.Hp);

        ctx.Service.Tick(nowTick: 1600);
        Assert.Equal(1000 - 15, mob.Hp);

        ctx.Service.Tick(nowTick: 1600 + 1500 + 50);
        Assert.Equal(1000 - 30, mob.Hp);
    }

    [Fact]
    public void Expiry_RemovesSc_AndCallsOnEnd()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.Agi = 10;

        ctx.Service.Start(pc, StatusType.IncreaseAgi, val1: 5, 0, 0, 0, durationMs: 500, nowTick: 0);
        Assert.Equal(15, pc.Stats.Agi);

        // Before expiry — still applied.
        ctx.Service.Tick(nowTick: 100);
        Assert.NotNull(ctx.Service.Get(pc, StatusType.IncreaseAgi));

        // Past expiry — auto-removed; revert applied.
        ctx.Service.Tick(nowTick: 600);
        Assert.Null(ctx.Service.Get(pc, StatusType.IncreaseAgi));
        Assert.Equal(10, pc.Stats.Agi);
    }

    [Fact]
    public void Refresh_OnReapply_RevertsAndApplies_NewVal()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.Agi = 10;

        ctx.Service.Start(pc, StatusType.IncreaseAgi, val1: 5, 0, 0, 0, durationMs: 10_000);
        Assert.Equal(15, pc.Stats.Agi);

        // Reapply with a different val1 — old must revert before new applies
        // (else stat mods would stack permanently).
        ctx.Service.Start(pc, StatusType.IncreaseAgi, val1: 8, 0, 0, 0, durationMs: 10_000);
        Assert.Equal(18, pc.Stats.Agi);
    }

    [Fact]
    public void HealOverTime_HealsButDoesNotExceedMaxHp()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.MaxHp = 100;
        pc.Stats.Hp = 90;

        ctx.Service.Start(pc, StatusType.HealOverTime, val1: 5, 0, 0, 0, durationMs: 10_000, nowTick: 0);

        ctx.Service.Tick(nowTick: 1100);
        Assert.Equal(95, pc.Stats.Hp);

        ctx.Service.Tick(nowTick: 2200);
        Assert.Equal(100, pc.Stats.Hp);

        // Cap holds — next periodic mustn't overflow.
        ctx.Service.Tick(nowTick: 3300);
        Assert.Equal(100, pc.Stats.Hp);
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
        var service = new StatusChangeService(damage, entities, new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);
        return new TestContext(service, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        StatusChangeService Service,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(short x, short y)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 1000 };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            Entities.Add(mob);
            return mob;
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
