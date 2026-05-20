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
/// T2.3 acceptance tests for the new <see cref="SkillImpl"/>
/// hierarchy (mirrors rathena-fork's per-skill class layout).
///
/// Each test exercises one skill via its dedicated subclass —
/// <c>Skills/Behaviors/&lt;Class&gt;/&lt;Skill&gt;.cs</c>. Skills
/// derived from <see cref="WeaponSkillImpl"/> are tested via
/// their overridden <see cref="WeaponSkillImpl.CalculateSkillRatio"/>
/// + <see cref="SkillImpl.ApplyAdditionalEffects"/> behavior;
/// status / splash skills exercise their respective hooks.
/// </summary>
public class SkillImplBehaviorTests
{
    // ---------- Registry ----------

    [Fact]
    public void Registry_IndexesAllRegisteredImpls()
    {
        var reg = new SkillBehaviorRegistry(new SkillImpl[]
        {
            new Map.Server.Skills.Behaviors.Swordman.Bash(),
            new Map.Server.Skills.Behaviors.Swordman.MagnumBreak(),
            new Map.Server.Skills.Behaviors.Knight.BowlingBash(),
            new Map.Server.Skills.Behaviors.Priest.LexDivina(),
            new Map.Server.Skills.Behaviors.Wizard.FrostNova(),
        });

        Assert.Equal(5, reg.Count);
        Assert.NotNull(reg.Get(SkillIds.SM_BASH));
        Assert.IsType<Map.Server.Skills.Behaviors.Swordman.Bash>(reg.Get(SkillIds.SM_BASH));
        Assert.IsType<Map.Server.Skills.Behaviors.Knight.BowlingBash>(reg.Get(SkillIds.KN_BOWLINGBASH));
        Assert.IsType<Map.Server.Skills.Behaviors.Priest.LexDivina>(reg.Get(SkillIds.PR_LEXDIVINA));
    }

    // ---------- Bash: ratio + hit + stun proc ----------

    [Fact]
    public void Bash_RatioBumpsBy30PerLevel()
    {
        var bash = new Map.Server.Skills.Behaviors.Swordman.Bash();
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        // SM_BASH: base 100 % + 30 % per skill level.
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
        // base 100, lv5 → 100 + 100*5*5/100 = 125.
        Assert.Equal(125, bash.ModifyHitRate(100, pc, mob, 5));
    }

    [Fact]
    public void Bash_StunProcsAtLv6Plus_OnLowRoll()
    {
        var bash = new Map.Server.Skills.Behaviors.Swordman.Bash(new FixedRandom(0));
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        bash.ApplyAdditionalEffects(pc, mob, 10, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(mob, StatusType.Stun));
    }

    [Fact]
    public void Bash_NoStunBelowLv6()
    {
        var bash = new Map.Server.Skills.Behaviors.Swordman.Bash(new FixedRandom(0));
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        bash.ApplyAdditionalEffects(pc, mob, 5, ctx.Behavior);
        Assert.Null(ctx.Sc.Get(mob, StatusType.Stun));
    }

    // ---------- WeaponSkillImpl pipeline runs full hit ----------

    [Fact]
    public void WeaponSkillImpl_PipelineComputesDamageAndApplies()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        pc.Stats.Batk = 100; pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100; pc.Stats.Hit = 200;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 9999; mob.Stats.MaxHp = 9999;

        // Bash routes through WeaponSkillImpl which calls
        // CalculateSkillRatio + ApplyAdditionalEffects automatically.
        var bash = new Map.Server.Skills.Behaviors.Swordman.Bash(new FixedRandom(99)); // no stun proc
        bash.CastendDamageId(pc, mob, 5, ctx.Behavior);

        Assert.True(mob.Hp < 9999);
    }

    // ---------- StatusSkillImpl: LexDivina toggle ----------

    [Fact]
    public void LexDivina_TogglesSilence()
    {
        var lex = new Map.Server.Skills.Behaviors.Priest.LexDivina();
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 51, 51);
        lex.CastendNoDamageId(pc, target, 3, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(target, StatusType.Silence));
        // Re-cast cures.
        lex.CastendNoDamageId(pc, target, 3, ctx.Behavior);
        Assert.Null(ctx.Sc.Get(target, StatusType.Silence));
    }

    // ---------- StatusSkillImpl: Cloaking toggle ----------

    [Fact]
    public void Cloaking_TogglesCloaking()
    {
        var cloak = new Map.Server.Skills.Behaviors.Assassin.Cloaking();
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        cloak.CastendNoDamageId(pc, pc, 5, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(pc, StatusType.Cloaking));
        cloak.CastendNoDamageId(pc, pc, 5, ctx.Behavior);
        Assert.Null(ctx.Sc.Get(pc, StatusType.Cloaking));
    }

    // ---------- RecursiveDamageSplashSkillImpl: MagnumBreak splash ----------

    [Fact]
    public void MagnumBreak_HitsSplashAndAppliesFireWeapon()
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

    // ---------- RecursiveDamageSplashSkillImpl: ArrowShower ----------

    [Fact]
    public void ArrowShower_HitsAllInSplash()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        pc.Stats.Batk = 100; pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100; pc.Stats.Hit = 200;
        var primary = ctx.AddMob(60, 60);
        var nearby = ctx.AddMob(61, 60);
        primary.Hp = primary.Stats.MaxHp = 1000;
        nearby.Hp = nearby.Stats.MaxHp = 1000;

        new Map.Server.Skills.Behaviors.Archer.ArrowShower().CastendDamageId(pc, primary, 5, ctx.Behavior);

        Assert.True(primary.Hp < 1000);
        Assert.True(nearby.Hp < 1000);
    }

    // ---------- Bowling Bash: primary + splash ----------

    [Fact]
    public void BowlingBash_HitsPrimaryAndSplashesNearby()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        pc.Stats.Batk = 100; pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100; pc.Stats.Hit = 200;
        var primary = ctx.AddMob(51, 51);
        var splash = ctx.AddMob(52, 52);
        var far = ctx.AddMob(80, 80);
        primary.Hp = primary.Stats.MaxHp = 1000;
        splash.Hp = splash.Stats.MaxHp = 1000;
        far.Hp = far.Stats.MaxHp = 1000;

        new Map.Server.Skills.Behaviors.Knight.BowlingBash().CastendDamageId(pc, primary, 5, ctx.Behavior);

        Assert.True(primary.Hp < 1000);
        Assert.True(splash.Hp < 1000);
        Assert.Equal(1000, far.Hp);
    }

    // ---------- Mage bolts: hit count scales with level ----------

    [Fact]
    public void FireBolt_HitsLevelTimes()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        pc.Stats.MatkMin = 100; pc.Stats.MatkMax = 100;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 9999; mob.Stats.MaxHp = 9999;
        new Map.Server.Skills.Behaviors.Mage.FireBolt().CastendDamageId(pc, mob, 5, ctx.Behavior);
        Assert.Equal(9999 - 100 * 5, mob.Hp);
    }

    // ---------- Priest Kyrie: shield = MaxHp% ----------

    [Fact]
    public void KyrieEleison_ShieldFromMaxHpPercentage()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var target = ctx.AddPlayer(2, 51, 51);
        target.MaxHp = target.Stats.MaxHp = 1000;
        new Map.Server.Skills.Behaviors.Priest.KyrieEleison().CastendNoDamageId(pc, target, 5, ctx.Behavior);
        var sc = ctx.Sc.Get(target, StatusType.Kyrie);
        Assert.NotNull(sc);
        Assert.Equal(220, sc!.Val1); // (12 + 2*5)% of 1000.
        Assert.Equal(10, sc.Val2);   // 5 + lv5 hits.
    }

    // ---------- Priest TurnUndead: instakill vs Undead at high chance ----------

    [Fact]
    public void TurnUndead_InstakillsAtHighChance()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        pc.Level = 99; pc.Stats.Luk = 100;
        var undead = ctx.AddMob(51, 51);
        undead.Stats.DefenseElement = BattleElement.Undead;
        undead.Level = 1;
        undead.Hp = 5000; undead.Stats.MaxHp = 5000;
        new Map.Server.Skills.Behaviors.Priest.TurnUndead(new FixedRandom(0)).CastendDamageId(pc, undead, 10, ctx.Behavior);
        Assert.Equal(0, undead.Hp);
    }

    [Fact]
    public void TurnUndead_NoOpVsNonUndead()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var living = ctx.AddMob(51, 51);
        living.Stats.DefenseElement = BattleElement.Neutral;
        living.Hp = 1000; living.Stats.MaxHp = 1000;
        new Map.Server.Skills.Behaviors.Priest.TurnUndead(new FixedRandom(0)).CastendDamageId(pc, living, 10, ctx.Behavior);
        Assert.Equal(1000, living.Hp);
    }

    // ---------- Thief Poison: roll proc ----------

    [Fact]
    public void Poison_AppliesPoisonOnLowRoll()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        var mob = ctx.AddMob(51, 51);
        new Map.Server.Skills.Behaviors.Thief.Poison(new FixedRandom(0)).CastendNoDamageId(pc, mob, 5, ctx.Behavior);
        Assert.NotNull(ctx.Sc.Get(mob, StatusType.Poison));
    }

    // ---------- Monk ExtremityFist: drains SP ----------

    [Fact]
    public void ExtremityFist_DrainsAllSp()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 50, 50);
        pc.Stats.Batk = 100; pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100; pc.Stats.Hit = 200;
        pc.Stats.Sp = 500;
        var mob = ctx.AddMob(51, 51);
        mob.Hp = 99999; mob.Stats.MaxHp = 99999;
        new Map.Server.Skills.Behaviors.Monk.ExtremityFist().CastendDamageId(pc, mob, 5, ctx.Behavior);
        Assert.Equal(0, pc.Stats.Sp);
        Assert.True(mob.Hp < 99999);
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
