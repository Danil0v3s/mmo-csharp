using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Status;

public class NaturalHealServiceTests
{
    [Fact]
    public void Pc_BelowMaxHp_RegensAfterInterval()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1);
        pc.MaxHp = 1000; pc.Hp = 500;
        pc.Stats.Vit = 10;

        ctx.Service.Tick(nowTick: 1);
        Assert.Equal(500, pc.Hp); // not yet due

        ctx.Service.Tick(nowTick: 6_500);
        // 1 + 1000/200 + 10/5 = 1 + 5 + 2 = 8
        Assert.Equal(508, pc.Hp);
    }

    [Fact]
    public void Pc_AtFullHp_DoesNothing()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1);
        pc.MaxHp = pc.Hp = 1000;

        ctx.Service.Tick(nowTick: 0);
        ctx.Service.Tick(nowTick: 10_000);

        Assert.Equal(1000, pc.Hp);
    }

    [Fact]
    public void Pc_Walking_SkipsHpRegen_StillRegensSp()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1);
        pc.MaxHp = 1000; pc.Hp = 500;
        pc.MaxSp = 100; pc.Sp = 50;
        pc.Stats.IntStat = 12;
        // Fake an in-flight walk so the regen path treats us as moving.
        pc.Walk = new Map.Server.Movement.WalkState(
            Array.Empty<Map.Server.Movement.Direction>(), 0, 0);

        ctx.Service.Tick(nowTick: 0);
        ctx.Service.Tick(nowTick: 10_000);

        Assert.Equal(500, pc.Hp); // walking blocks HP regen.
        // SP: 1 + 100/100 + 12/6 = 1 + 1 + 2 = 4
        // But walking skips SP too unless sitting (we don't sit).
        Assert.Equal(50, pc.Sp);
    }

    [Fact]
    public void DeadEntity_GetsNoRegen()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1);
        pc.MaxHp = 1000; pc.Hp = 0;

        ctx.Service.Tick(nowTick: 10_000);

        Assert.Equal(0, pc.Hp);
    }

    [Fact]
    public void Mob_RegensSameAsPc_NoWalkGate()
    {
        var ctx = Build();
        var mob = ctx.AddMob();
        mob.MaxHp = 1000; mob.Hp = 500;
        // CalcMob ran in AddMob — overwrite vit AFTER spawn so regen uses
        // the test value rather than the mob_db default of 1.
        mob.Stats.Vit = 10;

        // First observation only seeds the next-tick anchor — no regen yet.
        ctx.Service.Tick(nowTick: 0);
        ctx.Service.Tick(nowTick: 6_500);
        Assert.Equal(508, mob.Hp);
    }

    private static TestContext Build()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var ids = new EntityIdAllocator();
        var svc = new NaturalHealService(entities, NullLogger<NaturalHealService>.Instance);
        return new TestContext(svc, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        NaturalHealService Service,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, 100, 100);
            Entities.Add(pc);
            return pc;
        }
        public MobEntity AddMob()
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 1000 };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, 100, 100);
            new StatusCalcService().CalcMob(mob);
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
