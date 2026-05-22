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
/// ST.2 — verifies StatusOpsService forwards to the real services
/// instead of returning 0 / no-op (the prior stub behavior).
/// </summary>
public class StatusOpsServiceWiringTests
{
    [Fact]
    public void ChangeStart_ForwardsToStatusChangeService()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Stats.Str = 10;

        // SC_BLESSING type id is the StatusType.Blessing enum value.
        var rc = ctx.Ops.ChangeStart(src: pc, bl: pc, type: (int)StatusType.Blessing,
            rate: 10000, val1: 5, val2: 0, val3: 0, val4: 0, duration: 10_000, flag: 0);

        Assert.Equal(1, rc);
        Assert.NotNull(ctx.Sc.Get(pc, StatusType.Blessing));
        Assert.Equal(15, pc.Stats.Str); // OnStart applied
    }

    [Fact]
    public void ChangeEnd_ForwardsToStatusChangeService()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(2, 100, 100);
        ctx.Sc.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000);

        var rc = ctx.Ops.ChangeEnd(pc, type: (int)StatusType.Blessing, timerId: 0);

        Assert.Equal(1, rc);
        Assert.Null(ctx.Sc.Get(pc, StatusType.Blessing));
    }

    [Fact]
    public void ChangeClear_ForwardsToClearAll()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(3, 100, 100);
        ctx.Sc.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000);
        ctx.Sc.Start(pc, StatusType.Poison, 0, 0, 0, 0, 10_000);

        var cleared = ctx.Ops.ChangeClear(pc, type: 0);
        Assert.Equal(2, cleared);
    }

    [Fact]
    public void ChangeClearBuffs_ForwardsWithFlag()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(4, 100, 100);
        ctx.Sc.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000); // buff
        ctx.Sc.Start(pc, StatusType.Poison, 0, 0, 0, 0, 10_000);   // debuff

        // type=1 = Buffs only.
        ctx.Ops.ChangeClearBuffs(pc, type: (byte)SccbFlag.Buffs);

        Assert.Null(ctx.Sc.Get(pc, StatusType.Blessing));
        Assert.NotNull(ctx.Sc.Get(pc, StatusType.Poison));
    }

    [Fact]
    public void ChangeClearDebuffs_RemovesNegativeOnly()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(5, 100, 100);
        ctx.Sc.Start(pc, StatusType.Blessing, 5, 0, 0, 0, 10_000);
        ctx.Sc.Start(pc, StatusType.Poison, 0, 0, 0, 0, 10_000);
        ctx.Sc.Start(pc, StatusType.DecreaseAgi, 5, 0, 0, 0, 10_000);

        ctx.Ops.ChangeClearDebuffs(pc);

        Assert.NotNull(ctx.Sc.Get(pc, StatusType.Blessing));
        Assert.Null(ctx.Sc.Get(pc, StatusType.Poison));
        Assert.Null(ctx.Sc.Get(pc, StatusType.DecreaseAgi));
    }

    [Fact]
    public void CheckSkillUse_GatesOnCcSc()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(6, 100, 100);

        Assert.True(ctx.Ops.CheckSkillUse(pc, pc, skillId: 28, flag: 0));

        ctx.Sc.Start(pc, StatusType.Stun, 0, 0, 0, 0, 10_000);
        Assert.False(ctx.Ops.CheckSkillUse(pc, pc, skillId: 28, flag: 0));
    }

    [Fact]
    public void IsImmune_TrueForStatusImmuneMob()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(7, 100, 100);
        var mob = ctx.AddMob(101, 100);
        mob.Stats.Mode |= MobMode.StatusImmune;

        Assert.False(ctx.Ops.IsImmune(pc));
        Assert.True(ctx.Ops.IsImmune(mob));
    }

    [Fact]
    public void IsImmune_FalseForNormalMob()
    {
        var ctx = Build();
        var mob = ctx.AddMob(101, 100);

        Assert.False(ctx.Ops.IsImmune(mob));
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
