using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// T2.3 smoke tests for the rathena-fork <see cref="SkillImpl"/>
/// hierarchy. Validates the base architecture (registry, dispatch,
/// per-hook composition) using a handful of hand-implemented skills.
/// The bulk of the 1,100+ stubs under <c>Skills/Behaviors/</c>
/// don't have per-skill regression tests yet — those land
/// incrementally as each stub gets a real implementation.
/// </summary>
public class SkillImplBehaviorTests
{
    [Fact]
    public void Registry_IndexesSkillsById()
    {
        var reg = new SkillBehaviorRegistry(new SkillImpl[]
        {
            new Map.Server.Skills.Behaviors.Swordman.Bash(),
            new Map.Server.Skills.Behaviors.Swordman.MagnumBreak(),
        });
        Assert.Equal(2, reg.Count);
        Assert.NotNull(reg.Get(SkillIds.SM_BASH));
        Assert.IsType<Map.Server.Skills.Behaviors.Swordman.Bash>(reg.Get(SkillIds.SM_BASH));
    }

    [Fact]
    public void Bash_RatioBumpsBy30PerLevel()
    {
        var bash = new Map.Server.Skills.Behaviors.Swordman.Bash();
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        Assert.Equal(130, bash.CalculateSkillRatio(100, pc, mob, 1));
        Assert.Equal(250, bash.CalculateSkillRatio(100, pc, mob, 5));
        Assert.Equal(400, bash.CalculateSkillRatio(100, pc, mob, 10));
    }

    [Fact]
    public void Bash_HitRateBumpsPerLevel()
    {
        var bash = new Map.Server.Skills.Behaviors.Swordman.Bash();
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        Assert.Equal(125, bash.ModifyHitRate(100, pc, mob, 5));
    }

    [Fact]
    public void WeaponSkillImpl_PipelineComputesDamageAndApplies()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        pc.Stats.Batk = 100; pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100; pc.Stats.Hit = 200;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 9999; mob.Stats.MaxHp = 9999;
        var bash = new Map.Server.Skills.Behaviors.Swordman.Bash(new FixedRandom(99));
        bash.CastendDamageId(pc, mob, 5, ctx.Behavior);
        Assert.True(mob.Hp < 9999);
    }

    [Fact]
    public void MagnumBreak_SplashHitsAndAppliesFireWeapon()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        pc.Stats.Batk = 100; pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100; pc.Stats.Hit = 200;
        var near = ctx.AddMob(51, 51);
        var far = ctx.AddMob(80, 80);
        near.Hp = near.Stats.MaxHp = 5000;
        far.Hp = far.Stats.MaxHp = 5000;
        new Map.Server.Skills.Behaviors.Swordman.MagnumBreak().CastendDamageId(pc, near, 5, ctx.Behavior);
        Assert.True(near.Hp < 5000);
        Assert.Equal(5000, far.Hp);
        Assert.NotNull(ctx.Sc.Get(pc, StatusType.Fireweapon));
    }

    // ---------- Test rig ----------

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
        var battle = new BattleCalculator(new Random(0));
        var behaviorCtx = new SkillBehaviorContext(entities, damage, battle, sc);
        return new TestContext(behaviorCtx, sc, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        SkillBehaviorContext Behavior,
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

    private sealed class FixedRandom : Random
    {
        private readonly int _value;
        public FixedRandom(int value) { _value = value; }
        public override int Next(int maxValue) => _value % Math.Max(1, maxValue);
        public override int Next() => _value;
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
