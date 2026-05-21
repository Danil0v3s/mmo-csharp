using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Mob.Slaves;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.World;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.6 — unit tests for <see cref="SlaveMobService"/>. Covers
/// CountSlaves (mob_countslave), GetFriendByHpRate
/// (mob_getfriendhprate), GetMasterIfHpBelow (mob_getmasterhpltmaxrate).
/// </summary>
public class SlaveMobServiceTests
{
    [Fact]
    public void CountSlaves_NoSlaves_Zero()
    {
        var ctx = Build();
        var master = ctx.AddPlayer();
        Assert.Equal(0, ctx.Service.CountSlaves(master));
    }

    [Fact]
    public void CountSlaves_TwoSlavesOneDead_OnlyAlive()
    {
        var ctx = Build();
        var master = ctx.AddPlayer();
        var s1 = ctx.AddMob();
        s1.MasterId = master.Id;
        var s2 = ctx.AddMob();
        s2.MasterId = master.Id;
        var dead = ctx.AddMob();
        dead.MasterId = master.Id;
        dead.Hp = 0;
        Assert.Equal(2, ctx.Service.CountSlaves(master));
    }

    [Fact]
    public void CountSlaves_OtherMasterNotCounted()
    {
        var ctx = Build();
        var m1 = ctx.AddPlayer();
        var m2 = ctx.AddPlayer();
        var s1 = ctx.AddMob();
        s1.MasterId = m1.Id;
        var s2 = ctx.AddMob();
        s2.MasterId = m2.Id;
        Assert.Equal(1, ctx.Service.CountSlaves(m1));
        Assert.Equal(1, ctx.Service.CountSlaves(m2));
    }

    [Fact]
    public void GetFriendByHpRate_FindsWoundedAlly()
    {
        var ctx = Build();
        // Two wild mobs without masters are passive allies — used by
        // friend-heal mob_skill_db rows.
        var hunter = ctx.AddMob(x: 100, y: 100);
        var wounded = ctx.AddMob(x: 102, y: 100);
        wounded.MaxHp = 1000;
        wounded.Hp = 200;  // 20%
        var healthy = ctx.AddMob(x: 105, y: 100);
        healthy.MaxHp = 1000;
        healthy.Hp = 950;  // 95%

        // Friends with HP% 0..30 → wounded matches.
        var friend = ctx.Service.GetFriendByHpRate(hunter, 0, 30);
        Assert.Same(wounded, friend);
    }

    [Fact]
    public void GetFriendByHpRate_NoFriendInRange_ReturnsNull()
    {
        var ctx = Build();
        var hunter = ctx.AddMob(x: 100, y: 100);
        var farMob = ctx.AddMob(x: 150, y: 150);   // out of 8-cell range
        farMob.MaxHp = 1000;
        farMob.Hp = 100;
        Assert.Null(ctx.Service.GetFriendByHpRate(hunter, 0, 50));
    }

    [Fact]
    public void GetMasterIfHpBelow_ReturnsMaster_WhenHpLow()
    {
        var ctx = Build();
        var master = ctx.AddPlayer();
        master.MaxHp = 1000;
        master.Hp = 200;  // 20%
        var slave = ctx.AddMob();
        slave.MasterId = master.Id;
        // Master Hp < 50% → returned.
        Assert.Same(master, ctx.Service.GetMasterIfHpBelow(slave, 50));
    }

    [Fact]
    public void GetMasterIfHpBelow_AboveThreshold_ReturnsNull()
    {
        var ctx = Build();
        var master = ctx.AddPlayer();
        master.MaxHp = 1000;
        master.Hp = 800;  // 80%
        var slave = ctx.AddMob();
        slave.MasterId = master.Id;
        Assert.Null(ctx.Service.GetMasterIfHpBelow(slave, 50));
    }

    [Fact]
    public void GetMasterIfHpBelow_NoMaster_ReturnsNull()
    {
        var ctx = Build();
        var wildMob = ctx.AddMob();
        Assert.Null(ctx.Service.GetMasterIfHpBelow(wildMob, 100));
    }

    // --- harness ---

    private static TestContext Build()
    {
        const string mapName = "slave_test";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var service = new SlaveMobService(entities);
        return new TestContext(service, entities, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        SlaveMobService Service,
        EntityRegistry Entities,
        uint MapId)
    {
        private int _nextPcId = 1;
        private readonly EntityIdAllocator _ids = new();

        public PlayerEntity AddPlayer(short x = 100, short y = 100)
        {
            var charId = _nextPcId++;
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(short x = 100, short y = 100)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "M", Name = "M", Hp = 1000 };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(_ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            mob.MaxHp = mob.Hp = 1000;
            Entities.Add(mob);
            return mob;
        }
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
}
