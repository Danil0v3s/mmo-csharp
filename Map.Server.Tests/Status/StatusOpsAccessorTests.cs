using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Status.StatusOps;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Status;

/// <summary>
/// ST.6 — verifies the new IStatusOpsService accessor matrix:
/// companion-id reads (GetHomId / _Pet / _Merc / _Ele), HP/SP/Max
/// setters with overflow clamp, and the IsImmune branch.
/// </summary>
public class StatusOpsAccessorTests
{
    [Fact]
    public void GetCompanionIds_DefaultZeroWhenNoCompanion()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        Assert.Equal(0, ctx.Ops.GetHomId(pc));
        Assert.Equal(0, ctx.Ops.GetPetId(pc));
        Assert.Equal(0, ctx.Ops.GetMercId(pc));
        Assert.Equal(0, ctx.Ops.GetEleId(pc));
    }

    [Fact]
    public void SetHp_ClampsToMaxHp()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(2, 100, 100);
        pc.MaxHp = 500;
        ctx.Ops.SetHp(pc, 9999);
        Assert.Equal(500, pc.Hp);
    }

    [Fact]
    public void SetHp_ClampsToZero()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(3, 100, 100);
        ctx.Ops.SetHp(pc, -100);
        Assert.Equal(0, pc.Hp);
    }

    [Fact]
    public void SetMaxHp_MinimumOne()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(4, 100, 100);
        ctx.Ops.SetMaxHp(pc, 0);
        Assert.Equal(1, pc.MaxHp);
    }

    [Fact]
    public void SetSp_ClampsToMaxSp()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(5, 100, 100);
        pc.MaxSp = 100;
        ctx.Ops.SetSp(pc, 5000);
        Assert.Equal(100, pc.Sp);
    }

    [Fact]
    public void SetMaxSp_MinimumOne()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(6, 100, 100);
        ctx.Ops.SetMaxSp(pc, 0);
        Assert.Equal(1, pc.MaxSp);
    }

    [Fact]
    public void SetHp_OnMob_AlsoClamps()
    {
        var ctx = Build();
        var mob = ctx.AddMob(7, 100);
        mob.MaxHp = 500;
        ctx.Ops.SetHp(mob, 1000);
        Assert.Equal(500, mob.Hp);
    }

    [Fact]
    public void IsImmune_TrueForStatusImmuneMob()
    {
        var ctx = Build();
        var mob = ctx.AddMob(8, 100);
        mob.Stats.Mode |= MobMode.StatusImmune;
        Assert.True(ctx.Ops.IsImmune(mob));
    }

    [Fact]
    public void IsImmune_FalseForPlayer()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(9, 100, 100);
        Assert.False(ctx.Ops.IsImmune(pc));
    }

    [Fact]
    public void CheckSkillUse_GatesOnCcSc()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(10, 100, 100);
        Assert.True(ctx.Ops.CheckSkillUse(pc, pc, skillId: 28, flag: 0));
        ctx.Sc.Start(pc, StatusType.Sleep, 0, 0, 0, 0, 10_000);
        Assert.False(ctx.Ops.CheckSkillUse(pc, pc, skillId: 28, flag: 0));
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
        var sc = new StatusChangeService(damage, entities, new StatusEffectRegistry(),
            NullLogger<StatusChangeService>.Instance);
        var ops = new StatusOpsService(sc, NullLogger<StatusOpsService>.Instance);
        return new TestContext(ops, sc, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        IStatusOpsService Ops,
        StatusChangeService Sc,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            pc.Sp = pc.MaxSp = 200;
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
