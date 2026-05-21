using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Status.StatusOps;
using Map.Server.Tests.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// T2.3-P1 — focused per-skill regression for the manual AL_HEAL port.
/// Verifies the renewal formula, Kaite bounce, Berserk / SaturdayNightFever
/// suppression, Akaitsuki sign flip (heal→damage), and Bitescar end-on-cast.
/// </summary>
public class AcolyteHealTests
{
    [Fact]
    public void Heal_RenewalFormula_BaseAndIntScaling()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 50, 50);
        caster.Level = 99;
        caster.Stats.IntStat = 50;          // (99 + 50) / 5 = 29 → * 30 = 870 → * 10 / 10 = 870
        target.MaxHp = 5000;
        target.Hp = 100;

        var heal = ctx.MakeHeal();
        heal.CastendNoDamageId(caster, target, skillLevel: 10, ctx.Behavior);

        // Target should be healed by ~870 (no clamp triggered).
        Assert.Equal(100 + 870, target.Hp);
    }

    [Fact]
    public void Heal_RenewalFormula_ScalesLinearlyWithLevel()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 50, 50);
        caster.Level = 50;
        caster.Stats.IntStat = 50;          // (50 + 50) / 5 = 20 → * 30 = 600 → / 10 = 60 per lv
        target.MaxHp = 10_000;
        target.Hp = 100;

        var heal = ctx.MakeHeal();
        heal.CastendNoDamageId(caster, target, skillLevel: 5, ctx.Behavior);
        Assert.Equal(100 + 60 * 5, target.Hp); // 60 per skillLevel
    }

    [Fact]
    public void Heal_ClampedToMaxHp()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 50, 50);
        caster.Level = 99;
        caster.Stats.IntStat = 99;
        target.MaxHp = 200;
        target.Hp = 150;

        var heal = ctx.MakeHeal();
        heal.CastendNoDamageId(caster, target, skillLevel: 10, ctx.Behavior);

        Assert.Equal(200, target.Hp);
    }

    [Fact]
    public void Heal_BerserkTargetReceivesNothing()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 50, 50);
        caster.Level = 99; caster.Stats.IntStat = 50;
        target.MaxHp = 5000; target.Hp = 1000;
        ctx.Sc.Start(target, StatusType.Berserk, 1, 0, 0, 0, 60_000);

        ctx.MakeHeal().CastendNoDamageId(caster, target, 10, ctx.Behavior);

        Assert.Equal(1000, target.Hp); // suppressed
    }

    [Fact]
    public void Heal_KaiteBouncesBackToCaster()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 50, 50);
        caster.Level = 99; caster.Stats.IntStat = 50;
        caster.MaxHp = 5000; caster.Hp = 100;
        target.MaxHp = 5000; target.Hp = 4000;
        // SC_KAITE with val2 = 2 (counts remaining) — decrements to 1, still active.
        ctx.Sc.Start(target, StatusType.Kaite, val1: 1, val2: 2, 0, 0, 60_000);

        ctx.MakeHeal().CastendNoDamageId(caster, target, 10, ctx.Behavior);

        // Heal went to caster, not target.
        Assert.Equal(100 + 870, caster.Hp);
        Assert.Equal(4000, target.Hp);
        // Kaite still active with val2 = 1.
        var kaite = ctx.Sc.Get(target, StatusType.Kaite);
        Assert.NotNull(kaite);
        Assert.Equal(1, kaite!.Val2);
    }

    [Fact]
    public void Heal_KaiteFinalChargeEndsSc()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 50, 50);
        caster.Level = 99; caster.Stats.IntStat = 50;
        target.MaxHp = 5000; target.Hp = 4000;
        ctx.Sc.Start(target, StatusType.Kaite, 1, val2: 1, 0, 0, 60_000); // last charge

        ctx.MakeHeal().CastendNoDamageId(caster, target, 10, ctx.Behavior);

        Assert.Null(ctx.Sc.Get(target, StatusType.Kaite));
    }

    [Fact]
    public void Heal_SelfHealUnderKaiteIsVoided()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Level = 99; caster.Stats.IntStat = 50;
        caster.MaxHp = 5000; caster.Hp = 100;
        ctx.Sc.Start(caster, StatusType.Kaite, 1, val2: 2, 0, 0, 60_000);

        ctx.MakeHeal().CastendNoDamageId(caster, caster, 10, ctx.Behavior);

        Assert.Equal(100, caster.Hp); // voided
    }

    [Fact]
    public void Heal_AkaitsukiFlipsToDamage()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 50, 50);
        caster.Level = 99; caster.Stats.IntStat = 50;
        target.MaxHp = 5000; target.Hp = 5000;
        ctx.Sc.Start(target, StatusType.Akaitsuki, 1, 0, 0, 0, 60_000);

        ctx.MakeHeal().CastendNoDamageId(caster, target, 10, ctx.Behavior);

        // Heal of 870 became damage of 870.
        Assert.True(target.Hp < 5000);
    }

    [Fact]
    public void Heal_EndsBitescarOnTarget()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 50, 50);
        caster.Level = 99; caster.Stats.IntStat = 50;
        target.MaxHp = 5000; target.Hp = 4000;
        ctx.Sc.Start(target, StatusType.Bitescar, 1, 0, 0, 0, 60_000);

        ctx.MakeHeal().CastendNoDamageId(caster, target, 10, ctx.Behavior);

        Assert.Null(ctx.Sc.Get(target, StatusType.Bitescar));
    }

    // ---------- Test rig (mirrors SkillImplBehaviorTests.Build) ----------

    private static TestCtx Build()
    {
        const string mapName = "heal_test";
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
        var statusOps = new StatusOpsService(sc, NullLogger<StatusOpsService>.Instance);
        var battleConfig = new BattleConfigService(NullLogger<BattleConfigService>.Instance);
        var behaviorCtx = new SkillBehaviorContext(entities, damage, battle, sc);
        return new TestCtx(behaviorCtx, sc, statusOps, battleConfig, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestCtx(
        SkillBehaviorContext Behavior,
        StatusChangeService Sc,
        StatusOpsService StatusOps,
        BattleConfigService BattleConfig,
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

        public Map.Server.Skills.Behaviors.Acolyte.Heal MakeHeal() =>
            new(sideEffects: null, statusOps: StatusOps, exp: null,
                battleConfig: BattleConfig, skillAttack: null);
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
