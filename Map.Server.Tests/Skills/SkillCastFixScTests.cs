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

/// <summary>
/// T2.4b acceptance tests — <see cref="SkillCastTimingService.CastFixSc"/>
/// honors the SC overlays (Suffragium / Memorize / Slowcast / Paralysis /
/// Izayoi / Bragi). One test per overlay, plus a stacking check and the
/// Suffragium/Memorize consumption semantics.
/// </summary>
public class SkillCastFixScTests
{
    [Fact]
    public void NoSc_ReturnsInputTime()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        Assert.Equal(1000, ctx.Timing.CastFixSc(pc, 1000));
    }

    [Fact]
    public void Suffragium_HalvesAt_Lv3_AndConsumesOnCast()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        // Lv3 → 100 - 15*3 = 55 % multiplier.
        ctx.Sc.Start(pc, StatusType.Suffragium, val1: 3, 0, 0, 0, durationMs: 30_000);

        var t = ctx.Timing.CastFixSc(pc, 1000);
        Assert.Equal(550, t);

        // Consumed — subsequent cast is unaffected.
        Assert.Null(ctx.Sc.Get(pc, StatusType.Suffragium));
        Assert.Equal(1000, ctx.Timing.CastFixSc(pc, 1000));
    }

    [Fact]
    public void Memorize_HalvesCast_AndCountsDownVal1()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        ctx.Sc.Start(pc, StatusType.Memorize, val1: 2, 0, 0, 0, durationMs: 30_000);

        Assert.Equal(500, ctx.Timing.CastFixSc(pc, 1000));
        var still = ctx.Sc.Get(pc, StatusType.Memorize);
        Assert.NotNull(still);
        Assert.Equal(1, still!.Val1);

        Assert.Equal(500, ctx.Timing.CastFixSc(pc, 1000));
        Assert.Null(ctx.Sc.Get(pc, StatusType.Memorize)); // expired at val1=0.
    }

    [Fact]
    public void Slowcast_AddsTime_Lv5_PushesUp50Percent()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        // val1=5 → 100 + 10*5 = 150 % → 1.5×.
        ctx.Sc.Start(pc, StatusType.Slowcast, val1: 5, 0, 0, 0, durationMs: 30_000);

        Assert.Equal(1500, ctx.Timing.CastFixSc(pc, 1000));

        // Permanent for SC duration — repeated calls don't consume.
        Assert.NotNull(ctx.Sc.Get(pc, StatusType.Slowcast));
        Assert.Equal(1500, ctx.Timing.CastFixSc(pc, 1000));
    }

    [Fact]
    public void Paralysis_Val3_PushesCastUp()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        // val3=20 → 120 % → 1.2×.
        ctx.Sc.Start(pc, StatusType.Paralysis, val1: 1, val2: 0, val3: 20, val4: 0, durationMs: 30_000);

        Assert.Equal(1200, ctx.Timing.CastFixSc(pc, 1000));
    }

    [Fact]
    public void Izayoi_HalvesCast()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        ctx.Sc.Start(pc, StatusType.Izayoi, val1: 1, 0, 0, 0, durationMs: 30_000);

        Assert.Equal(500, ctx.Timing.CastFixSc(pc, 1000));
    }

    [Fact]
    public void Poembragi_Val2_ScalesCastDown()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        // val2=30 → time × (100-30)/100 = 70 %.
        ctx.Sc.Start(pc, StatusType.Poembragi, val1: 1, val2: 30, val3: 0, val4: 0, durationMs: 30_000);

        Assert.Equal(700, ctx.Timing.CastFixSc(pc, 1000));
    }

    [Fact]
    public void Suffragium_AndSlowcast_StackInOrder_DebuffFirst_BuffSecond()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        // Slowcast lv2 first → 100 + 20 = 120 % (1.2×)
        // Then Suffragium lv3 → 1200 × 55 / 100 = 660.
        ctx.Sc.Start(pc, StatusType.Slowcast, val1: 2, 0, 0, 0, durationMs: 30_000);
        ctx.Sc.Start(pc, StatusType.Suffragium, val1: 3, 0, 0, 0, durationMs: 30_000);

        Assert.Equal(660, ctx.Timing.CastFixSc(pc, 1000));
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
        var sc = new StatusChangeService(damage, entities, new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);

        var skillDb = new SkillDb();
        var cfg = new BattleConfigService(NullLogger<BattleConfigService>.Instance);
        var timing = new SkillCastTimingService(skillDb, cfg, sc);
        return new TestContext(timing, sc, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        SkillCastTimingService Timing,
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

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string n) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
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
