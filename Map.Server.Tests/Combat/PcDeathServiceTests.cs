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

public class PcDeathServiceTests
{
    [Fact]
    public void OnPcDead_MarksDead_DeductsExp_StopsAttack()
    {
        var (svc, attack, pc) = New();
        pc.BaseExp = 10_000;
        pc.JobExp = 5_000;
        // Pretend the PC was mid-attack.
        attack.StartAttack(pc, new EntityId(99999), continuous: true);

        svc.OnPcDead(pc, source: null);

        Assert.True(svc.IsDead(pc));
        Assert.Equal(9_900, pc.BaseExp); // -1% rAthena death penalty
        Assert.Equal(4_950, pc.JobExp);
        Assert.Null(pc.Attack);
    }

    [Fact]
    public void Respawn_RestoresFullHpSp_ClearsDeadFlag()
    {
        var (svc, _, pc) = New();
        pc.MaxHp = 1000; pc.Hp = 0;
        pc.MaxSp = 200; pc.Sp = 0;

        svc.OnPcDead(pc, source: null);
        svc.Respawn(pc);

        Assert.False(svc.IsDead(pc));
        Assert.Equal(1000, pc.Hp);
        Assert.Equal(200, pc.Sp);
    }

    [Fact]
    public void OnPcDead_Idempotent()
    {
        var (svc, _, pc) = New();
        pc.BaseExp = 1000;
        svc.OnPcDead(pc, source: null);
        svc.OnPcDead(pc, source: null);
        // EXP penalty applied only once (1000 → 990).
        Assert.Equal(990, pc.BaseExp);
    }

    private static (PcDeathService svc, IAttackService attack, PlayerEntity pc) New()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(), NullLogger<MovementService>.Instance);
        var mobDb = new StubMobDb();
        var ids = new EntityIdAllocator();
        var itemCatalog = new EmptyItemCatalog();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(new MobSpawnRegistry(), entities, world, mobDb,
            itemCatalog, itemDrops, movement, visibility, ids, new StatusCalcService(),
            NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var attack = new AttackService(entities, damage, movement, NullLogger<AttackService>.Instance);
        var svc = new PcDeathService(attack, visibility, NullLogger<PcDeathService>.Instance);

        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), (uint)mapName.GetHashCode(), 100, 100);
        pc.MaxHp = pc.Hp = 1000;
        entities.Add(pc);
        return (svc, attack, pc);
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
