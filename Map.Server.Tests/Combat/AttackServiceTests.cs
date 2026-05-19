using Core.Server.Packets.Out.ZC;
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

namespace Map.Server.Tests.Combat;

public class AttackServiceTests
{
    [Fact]
    public void StartAttack_OutOfRange_TriggersChase_NoSwing()
    {
        var ctx = Build();
        var attacker = ctx.AddPlayer(50, 50, charId: 1);
        var target = ctx.AddMob(60, 50, hp: 100); // 10 cells away — out of melee range
        attacker.Stats.AttackRange = 1;
        attacker.Stats.Adelay = 1000;

        Assert.True(ctx.AttackService.StartAttack(attacker, target.Id, continuous: true));
        ctx.AttackService.Tick(0);

        // Should NOT have dealt any damage yet; should have started walking.
        Assert.Equal(100, target.Hp);
        Assert.NotNull(attacker.Walk);
        Assert.Empty(ctx.Dispatcher.Sent.Where(p => p.packet is ZC_NOTIFY_ACT3));
    }

    [Fact]
    public void StartAttack_InRange_FiresSwingAndScheduleNext()
    {
        var ctx = Build();
        var attacker = ctx.AddPlayer(50, 50, charId: 1);
        var target = ctx.AddMob(51, 50, hp: 100);
        attacker.Stats.AttackRange = 1;
        attacker.Stats.WatkMin = attacker.Stats.WatkMax = 20;
        attacker.Stats.Hit = 200;
        attacker.Stats.Adelay = 1000;
        // Disable crits so test is deterministic
        attacker.Stats.Cri = 0;

        Assert.True(ctx.AttackService.StartAttack(attacker, target.Id, continuous: true));
        ctx.AttackService.Tick(nowTick: 100);

        Assert.True(target.Hp < 100); // damage dealt
        Assert.Single(ctx.Dispatcher.Sent.Where(p => p.packet is ZC_NOTIFY_ACT3));
        // Schedule pushed out by adelay (1000 ms).
        Assert.Equal(100 + 1000, attacker.Attack!.AttackableTick);
    }

    [Fact]
    public void Continuous_Stops_WhenTargetDies()
    {
        var ctx = Build();
        var attacker = ctx.AddPlayer(50, 50, charId: 1);
        var target = ctx.AddMob(51, 50, hp: 5);
        attacker.Stats.AttackRange = 1;
        attacker.Stats.WatkMin = attacker.Stats.WatkMax = 1000;
        attacker.Stats.Hit = 200;
        attacker.Stats.Adelay = 1000;
        attacker.Stats.Cri = 0;

        ctx.AttackService.StartAttack(attacker, target.Id, continuous: true);
        ctx.AttackService.Tick(nowTick: 100);

        // Target died — attack state must clear.
        Assert.Null(attacker.Attack);
    }

    [Fact]
    public void SingleAttack_StopsAfterOneSwing()
    {
        var ctx = Build();
        var attacker = ctx.AddPlayer(50, 50, charId: 1);
        var target = ctx.AddMob(51, 50, hp: 10000);
        attacker.Stats.AttackRange = 1;
        attacker.Stats.WatkMin = attacker.Stats.WatkMax = 10;
        attacker.Stats.Hit = 200;
        attacker.Stats.Adelay = 1000;
        attacker.Stats.Cri = 0;

        ctx.AttackService.StartAttack(attacker, target.Id, continuous: false);
        ctx.AttackService.Tick(nowTick: 100);

        Assert.Null(attacker.Attack);
        Assert.True(target.Hp < 10000);
    }

    // ---- harness ----

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
        var attack = new AttackService(entities, damage, movement, NullLogger<AttackService>.Instance);
        return new TestContext(attack, entities, dispatcher, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        AttackService AttackService,
        EntityRegistry Entities,
        RecordingDispatcher Dispatcher,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(short x, short y, int charId)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Stats.MaxHp = pc.Stats.Hp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(short x, short y, int hp)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = hp };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            mob.MaxHp = hp;
            mob.Hp = hp;
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
