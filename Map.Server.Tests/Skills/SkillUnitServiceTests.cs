using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

public class SkillUnitServiceTests
{
    [Fact]
    public void Place_StormGust_CreatesGroup_WithFullSquareOfUnits()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 100, 100);
        caster.Stats.MatkMin = caster.Stats.MatkMax = 100;
        var group = ctx.Units.Place(caster, SkillIds.WZ_STORMGUST, 5, 100, 100);

        Assert.NotNull(group);
        // Storm Gust radius = 5 → (2*5+1)^2 = 121 unit cells.
        Assert.Equal(11 * 11, group!.Units.Count);
    }

    [Fact]
    public void Place_Magnus_TicksAndDamagesMobOnCell()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 100, 100);
        caster.Stats.MatkMin = caster.Stats.MatkMax = 200;
        // Place at (105, 105) with radius 2 — covers (103-107, 103-107).
        var group = ctx.Units.Place(caster, SkillIds.PR_MAGNUSEXORCISMUS, 5, 105, 105);
        Assert.NotNull(group);

        var mob = ctx.AddMob(105, 105, hp: 10000);
        var startingHp = mob.Hp;

        // Magnus interval = 1000ms; tick past the first boundary.
        ctx.Units.Tick(nowTick: Environment.TickCount64 + 1100);

        Assert.True(mob.Hp < startingHp, $"expected damage; mob hp {mob.Hp}");
    }

    [Fact]
    public void UnknownSkill_PlaceReturnsNull()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 100, 100);
        Assert.Null(ctx.Units.Place(caster, skillId: 9999, skillLevel: 1, 100, 100));
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
        var units = new SkillUnitService(entities, damage, NullLogger<SkillUnitService>.Instance);
        return new TestContext(units, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        SkillUnitService Units,
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

        public MobEntity AddMob(short x, short y, int hp)
        {
            var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = hp };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            mob.MaxHp = hp; mob.Hp = hp;
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
