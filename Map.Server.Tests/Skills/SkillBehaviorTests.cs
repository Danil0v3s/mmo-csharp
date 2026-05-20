using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Resolvers;
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
/// T2.5 acceptance tests — <see cref="ISkillBehavior"/> registry +
/// the two seed plugins (MagnumBreak, Bash).
/// </summary>
public class SkillBehaviorTests
{
    // ---------- Registry plumbing ----------

    [Fact]
    public void Registry_IndexesPluginsBySkillId()
    {
        var reg = new SkillBehaviorRegistry(new ISkillBehavior[]
        {
            new MagnumBreakBehavior(),
            new BashBehavior(),
        });

        Assert.Equal(2, reg.Count);
        Assert.NotNull(reg.Get(SkillIds.SM_MAGNUM));
        Assert.NotNull(reg.Get(SkillIds.SM_BASH));
        Assert.Null(reg.Get(SkillIds.AL_HEAL));      // no plugin registered.
    }

    [Fact]
    public void EmptyRegistry_GetReturnsNull_AndCountZero()
    {
        var reg = new SkillBehaviorRegistry(Array.Empty<ISkillBehavior>());
        Assert.Equal(0, reg.Count);
        Assert.Null(reg.Get(SkillIds.SM_MAGNUM));
    }

    // ---------- MagnumBreak ----------

    [Fact]
    public void MagnumBreak_HitsEveryEnemyInSplash_AndAppliesFireWeapon()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        // Give the caster real weapon stats so the splash swing > 0.
        caster.Stats.Batk = 100;
        caster.Stats.WatkMin = 100;
        caster.Stats.WatkMax = 100;
        caster.Stats.Hit = 200;

        var nearMob = ctx.AddMob(51, 51);  // within radius 2
        var farMob = ctx.AddMob(80, 80);   // way outside

        nearMob.Hp = 1000; nearMob.Stats.MaxHp = 1000;
        farMob.Hp = 1000; farMob.Stats.MaxHp = 1000;

        // Test-cast lv5 → 120 + 20*5 = 220 % rate. Generic resolver applies
        // the rate; we only assert that mobs IN radius took damage and the
        // far mob did NOT.
        var def = MakeDef(SkillIds.SM_MAGNUM, damageKind: SkillDamageKind.Weapon, range: 0);
        var plugin = new MagnumBreakBehavior();
        var handled = plugin.Resolve(caster, nearMob, def, skillLevel: 5, ctx.Behavior);

        Assert.True(handled);                                    // plugin claimed cast.
        Assert.True(nearMob.Hp < 1000, $"nearMob HP should drop: {nearMob.Hp}");
        Assert.Equal(1000, farMob.Hp);                           // out of splash.
        Assert.NotNull(ctx.Sc.Get(caster, StatusType.Fireweapon)); // endow applied.
    }

    [Fact]
    public void MagnumBreak_SkipsSelf_EvenIfInRadius()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        caster.Hp = 1000; caster.Stats.MaxHp = 1000;
        // Just the caster; no mobs. The plugin should still apply SC_FIREWEAPON
        // and NOT damage the caster.
        var def = MakeDef(SkillIds.SM_MAGNUM, damageKind: SkillDamageKind.Weapon, range: 0);
        new MagnumBreakBehavior().Resolve(caster, caster, def, skillLevel: 1, ctx.Behavior);

        Assert.Equal(1000, caster.Hp);
        Assert.NotNull(ctx.Sc.Get(caster, StatusType.Fireweapon));
    }

    [Fact]
    public void MagnumBreak_NoEnemiesInRadius_StillAppliesFireWeapon()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var def = MakeDef(SkillIds.SM_MAGNUM, damageKind: SkillDamageKind.Weapon, range: 0);

        var handled = new MagnumBreakBehavior().Resolve(caster, caster, def, skillLevel: 1, ctx.Behavior);

        Assert.True(handled);
        Assert.NotNull(ctx.Sc.Get(caster, StatusType.Fireweapon));
    }

    // ---------- Bash ----------

    [Fact]
    public void Bash_FallsThroughToGenericResolver_ReturnsFalse()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddMob(51, 51);
        target.Hp = 1000; target.Stats.MaxHp = 1000;

        var def = MakeDef(SkillIds.SM_BASH, damageKind: SkillDamageKind.Weapon, range: 1);
        // lv 5 → no stun proc; plugin returns false (fall-through).
        var handled = new BashBehavior(new Random(0)).Resolve(caster, target, def, skillLevel: 5, ctx.Behavior);

        Assert.False(handled);
        // Plugin shouldn't apply stun at lv 5.
        Assert.Null(ctx.Sc.Get(target, StatusType.Stun));
    }

    [Fact]
    public void Bash_AtLv10_AppliesStun_WhenRollUnderChance()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddMob(51, 51);
        target.Hp = 1000; target.Stats.MaxHp = 1000;

        // RngStub Next(100) returns 0 → 0 < 30 → stun fires at lv10.
        var def = MakeDef(SkillIds.SM_BASH, damageKind: SkillDamageKind.Weapon, range: 1);
        new BashBehavior(new FixedRandom(0)).Resolve(caster, target, def, skillLevel: 10, ctx.Behavior);

        Assert.NotNull(ctx.Sc.Get(target, StatusType.Stun));
    }

    [Fact]
    public void Bash_AtLv10_RollOverChance_NoStun()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddMob(51, 51);
        target.Hp = 1000; target.Stats.MaxHp = 1000;

        // FixedRandom(99) → 99 >= 30 (lv10 chance) → no stun.
        var def = MakeDef(SkillIds.SM_BASH, damageKind: SkillDamageKind.Weapon, range: 1);
        new BashBehavior(new FixedRandom(99)).Resolve(caster, target, def, skillLevel: 10, ctx.Behavior);

        Assert.Null(ctx.Sc.Get(target, StatusType.Stun));
    }

    [Fact]
    public void Bash_BelowLv6_NoStunProc()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddMob(51, 51);
        target.Hp = 1000; target.Stats.MaxHp = 1000;

        // Lv 5 is below the Fatal Blow threshold; even a 0 roll shouldn't
        // proc since the chance is 0 %.
        new BashBehavior(new FixedRandom(0)).Resolve(caster, target,
            MakeDef(SkillIds.SM_BASH, damageKind: SkillDamageKind.Weapon, range: 1),
            skillLevel: 5, ctx.Behavior);

        Assert.Null(ctx.Sc.Get(target, StatusType.Stun));
    }

    // ---------- Test rig ----------

    private static SkillDefinition MakeDef(ushort id, SkillDamageKind damageKind, short range)
        => new()
        {
            Id = id,
            Name = $"skill_{id}",
            MaxLevel = 10,
            DamageKind = damageKind,
            Target = SkillTargetMode.TargetEnemy,
            Range = range,
            DamageRate = new[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
        };

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
