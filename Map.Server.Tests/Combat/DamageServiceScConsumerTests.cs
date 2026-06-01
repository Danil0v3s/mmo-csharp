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

/// <summary>
/// T2.4b+ acceptance tests — combat-side reads of the SC presence
/// markers that StatusEffectRegistry now attaches. Damage flowing
/// through <see cref="DamageService.ApplyDamage"/> on a target with
/// SteelBody / Kyrie / AutoGuard active honors the SC overlay.
/// </summary>
public class DamageServiceScConsumerTests
{
    [Fact]
    public void NoSc_BaselineDamage_PassesThrough()
    {
        var ctx = Build(blockRoll: 50);
        var mob = ctx.AddMob(100, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        ctx.Damage.ApplyDamage(mob, 200);

        Assert.Equal(800, mob.Hp);
    }

    [Fact]
    public void SteelBody_Reduces_DamageBy90Percent()
    {
        var ctx = Build();
        var mob = ctx.AddMob(100, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        ctx.Sc.Start(mob, StatusType.Steelbody, val1: 1, 0, 0, 0, durationMs: 30_000);
        ctx.Damage.ApplyDamage(mob, 200);

        // 200 * 0.1 = 20; with the floor-at-1 the formula yields 20.
        Assert.Equal(1000 - 20, mob.Hp);
    }

    [Fact]
    public void SteelBody_FloorAt1_OnTinyHit()
    {
        var ctx = Build();
        var mob = ctx.AddMob(100, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        ctx.Sc.Start(mob, StatusType.Steelbody, val1: 1, 0, 0, 0, durationMs: 30_000);
        // damage=5 → 5/10 = 0, but floor-at-1 keeps a register so the
        // client still animates a hit.
        ctx.Damage.ApplyDamage(mob, 5);
        Assert.Equal(999, mob.Hp);
    }

    [Fact]
    public void Kyrie_AbsorbsDamage_AndDecrementsShield()
    {
        var ctx = Build();
        var mob = ctx.AddMob(100, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        // Val1=150 shield, Val2=5 hits remaining.
        ctx.Sc.Start(mob, StatusType.Kyrie, val1: 150, val2: 5, val3: 0, val4: 0, durationMs: 30_000);

        ctx.Damage.ApplyDamage(mob, 100);

        // Full 100 absorbed → HP unchanged, shield 50, hits 4.
        Assert.Equal(1000, mob.Hp);
        var sc = ctx.Sc.Get(mob, StatusType.Kyrie);
        Assert.NotNull(sc);
        Assert.Equal(50, sc!.Val1);
        Assert.Equal(4, sc.Val2);
    }

    [Fact]
    public void Kyrie_PartialAbsorb_ResidualHitsHp()
    {
        var ctx = Build();
        var mob = ctx.AddMob(100, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        // Val1=30 shield, Val2=5 hits.
        ctx.Sc.Start(mob, StatusType.Kyrie, val1: 30, val2: 5, val3: 0, val4: 0, durationMs: 30_000);

        ctx.Damage.ApplyDamage(mob, 100);

        // 30 absorbed, 70 to HP. Shield → 0, SC ends.
        Assert.Equal(1000 - 70, mob.Hp);
        Assert.Null(ctx.Sc.Get(mob, StatusType.Kyrie));
    }

    [Fact]
    public void Kyrie_ExpiresOnLastHit_WhenHitCounterReachesZero()
    {
        var ctx = Build();
        var mob = ctx.AddMob(100, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        // Val1=1000 shield, Val2=2 hits.
        ctx.Sc.Start(mob, StatusType.Kyrie, val1: 1000, val2: 2, val3: 0, val4: 0, durationMs: 30_000);

        ctx.Damage.ApplyDamage(mob, 50);
        ctx.Damage.ApplyDamage(mob, 50);

        // Both absorbed; hits exhausted on the 2nd → SC ends.
        Assert.Equal(1000, mob.Hp);
        Assert.Null(ctx.Sc.Get(mob, StatusType.Kyrie));
    }

    [Fact]
    public void Autoguard_BlocksFully_WhenRollUnderChance()
    {
        // Random.Next(100) → 0 (always). Val1=50 → 0 < 50 → block.
        var ctx = Build(blockRoll: 0);
        var mob = ctx.AddMob(100, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        ctx.Sc.Start(mob, StatusType.Autoguard, val1: 50, 0, 0, 0, durationMs: 30_000);
        ctx.Damage.ApplyDamage(mob, 200);

        Assert.Equal(1000, mob.Hp); // fully blocked.
    }

    [Fact]
    public void Autoguard_FailsBlock_WhenRollAboveChance()
    {
        // Random.Next(100) → 99 (never). Val1=50 → 99 >= 50 → no block.
        var ctx = Build(blockRoll: 99);
        var mob = ctx.AddMob(100, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        ctx.Sc.Start(mob, StatusType.Autoguard, val1: 50, 0, 0, 0, durationMs: 30_000);
        ctx.Damage.ApplyDamage(mob, 200);

        Assert.Equal(800, mob.Hp);
    }

    [Fact]
    public void Kyrie_AndSteelBody_StackInOrder_KyrieFirst()
    {
        var ctx = Build(blockRoll: 99);
        var mob = ctx.AddMob(100, 100);
        mob.Stats.MaxHp = 1000;
        mob.Hp = 1000;

        ctx.Sc.Start(mob, StatusType.Kyrie, val1: 50, val2: 1, val3: 0, val4: 0, durationMs: 30_000);
        ctx.Sc.Start(mob, StatusType.Steelbody, val1: 1, 0, 0, 0, durationMs: 30_000);

        // 200 hit → Kyrie absorbs 50, 150 remaining → SteelBody /10 = 15.
        ctx.Damage.ApplyDamage(mob, 200);

        Assert.Equal(1000 - 15, mob.Hp);
        // Kyrie shield exhausted (50 absorbed, Val1=0) → SC ended.
        Assert.Null(ctx.Sc.Get(mob, StatusType.Kyrie));
    }

    // ---------- Test rig ----------

    private static TestContext Build(int blockRoll = 0)
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

        // Two-phase build: SC service first (with a placeholder damage service);
        // then real damage service that holds the SC. Replace via field once both exist.
        var fakeDamage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var sc = new StatusChangeService(fakeDamage, entities, new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance,
            sc: new System.Lazy<IStatusChangeService>(() => sc), rng: new FixedRandom(blockRoll));

        return new TestContext(damage, sc, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        DamageService Damage,
        StatusChangeService Sc,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
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

    /// <summary>Deterministic stub — <c>Next(100)</c> returns a fixed
    /// value so block-roll tests are reproducible.</summary>
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
