using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Mob.Conditions;
using Map.Server.Movement;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Mob;

/// <summary>
/// MOBAI-03 — the four target-switch mode bits (MD_TARGETWEAK, MD_CHANGECHASE,
/// MD_RANDOMTARGET, plus the proactive attacker-switch) driven from the hard-AI tick.
/// Mirrors rAthena mob.cpp:1309 (TARGETWEAK), :1881 (changechase), :1993 (randomtarget),
/// :1785-1851 (attacker arm).
/// </summary>
public class MobChangeTargetModeTests
{
    // --- TARGETWEAK (mob.cpp:1309 — skip targets not at least 5 levels weaker) ---

    [Fact]
    public void TargetWeak_ignores_pc_within_five_levels()
    {
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 50,
            "Aggressive", "CanAttack", "CanMove", "TargetWeak");
        var pc = ctx.AddPlayer(52, 50, 1, level: 46); // within 5 of mob (50-5=45 → 46 >= 45 → skip)

        ctx.Ai.Tick(1000);

        Assert.Null(mob.Attack); // not aggroed
    }

    [Fact]
    public void TargetWeak_aggros_pc_more_than_five_levels_weaker()
    {
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 50,
            "Aggressive", "CanAttack", "CanMove", "TargetWeak");
        var pc = ctx.AddPlayer(52, 50, 1, level: 44); // 44 < 50-5=45 → eligible

        ctx.Ai.Tick(1000);

        Assert.NotNull(mob.Attack);
        Assert.Equal(pc.Id.Value, mob.Attack!.TargetId.Value);
    }

    [Fact]
    public void Without_TargetWeak_aggros_regardless_of_level()
    {
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 50,
            "Aggressive", "CanAttack", "CanMove"); // no TargetWeak
        var pc = ctx.AddPlayer(52, 50, 1, level: 49); // would be skipped if TargetWeak

        ctx.Ai.Tick(1000);

        Assert.NotNull(mob.Attack);
        Assert.Equal(pc.Id.Value, mob.Attack!.TargetId.Value);
    }

    // --- CHANGECHASE (mob.cpp:1881 — chasing mob switches to an enemy in melee reach) ---

    [Fact]
    public void ChangeChase_in_follow_switches_to_enemy_in_melee_range()
    {
        var ctx = Build();
        // Non-aggressive ChangeChase mob mid-chase: TargetId set (chasing A), no live AttackState,
        // FOLLOW state → reaches the changechase else-if.
        var mob = ctx.AddMob(50, 50, level: 30, "CanAttack", "CanMove", "ChangeChase");
        var far = ctx.AddPlayer(55, 50, 1, level: 30);   // dist 5 — the original chase target
        var near = ctx.AddPlayer(51, 50, 2, level: 30);  // dist 1 — steps into melee reach
        mob.TargetId = far.Id.Value;
        mob.SkillState = MobSkillState.Follow;

        ctx.Ai.Tick(1000);

        Assert.Equal(near.Id.Value, mob.TargetId); // switched to the in-melee enemy
    }

    [Fact]
    public void ChangeChase_in_rush_switches_directly_without_the_changetargetchase_bit()
    {
        // MOBAI-07 — rAthena mob_ai_sub_hard_changechase sets the in-reach enemy DIRECTLY
        // (md->target_id = bl->id); it does NOT run mob_can_changetarget. So a RUSH-state mob with
        // MD_CHANGECHASE switches even though it lacks MD_CHANGETARGETCHASE (which the normal
        // can-change-target gate would require for the RUSH state).
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 30, "CanAttack", "CanMove", "ChangeChase"); // no ChangeTargetChase
        var far = ctx.AddPlayer(55, 50, 1, level: 30);   // the original chase target (dist 5)
        var near = ctx.AddPlayer(51, 50, 2, level: 30);  // steps into melee reach (dist 1)
        mob.TargetId = far.Id.Value;
        mob.SkillState = MobSkillState.Rush;

        ctx.Ai.Tick(1000);

        Assert.Equal(near.Id.Value, mob.TargetId); // switched directly despite no ChangeTargetChase bit
    }

    [Fact]
    public void Without_ChangeChase_bit_keeps_chasing_original_target()
    {
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 30, "CanAttack", "CanMove"); // no ChangeChase
        var far = ctx.AddPlayer(55, 50, 1, level: 30);
        var near = ctx.AddPlayer(51, 50, 2, level: 30);
        mob.TargetId = far.Id.Value;
        mob.SkillState = MobSkillState.Follow;

        ctx.Ai.Tick(1000);

        Assert.Equal(far.Id.Value, mob.TargetId); // unchanged
    }

    [Fact]
    public void ChangeChase_skips_a_hidden_enemy_in_reach()
    {
        // AI-CHANGECHASE-VIS — rAthena status_check_skilluse: a hidden/cloaked enemy isn't perceivable,
        // so the mob doesn't changechase onto it even though it's standing in melee reach.
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 30, "CanAttack", "CanMove", "ChangeChase");
        var far = ctx.AddPlayer(55, 50, 1, level: 30);
        var nearHidden = ctx.AddPlayer(51, 50, 2, level: 30);
        ctx.Sc.Start(nearHidden, StatusType.Hiding, val1: 1, 0, 0, 0, durationMs: 60_000, nearHidden);
        mob.TargetId = far.Id.Value;
        mob.SkillState = MobSkillState.Follow;

        ctx.Ai.Tick(1000);

        Assert.Equal(far.Id.Value, mob.TargetId); // hidden enemy ignored → keeps chasing the original
    }

    [Fact]
    public void ChangeChase_switches_to_a_visible_enemy_in_reach()
    {
        // Control for the hide test: a non-hidden enemy in reach is still changechased onto.
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 30, "CanAttack", "CanMove", "ChangeChase");
        var far = ctx.AddPlayer(55, 50, 1, level: 30);
        var near = ctx.AddPlayer(51, 50, 2, level: 30); // visible
        mob.TargetId = far.Id.Value;
        mob.SkillState = MobSkillState.Follow;

        ctx.Ai.Tick(1000);

        Assert.Equal(near.Id.Value, mob.TargetId);
    }

    [Fact]
    public void ChangeChase_does_not_switch_outside_rush_or_follow()
    {
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 30, "CanAttack", "CanMove", "ChangeChase");
        var far = ctx.AddPlayer(55, 50, 1, level: 30);
        var near = ctx.AddPlayer(51, 50, 2, level: 30);
        mob.TargetId = far.Id.Value;
        mob.SkillState = MobSkillState.Idle; // not Rush/Follow → no changechase

        ctx.Ai.Tick(1000);

        Assert.Equal(far.Id.Value, mob.TargetId);
    }

    [Fact]
    public void TryChangeChase_returns_only_an_enemy_within_reach()
    {
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 30, "CanAttack", "CanMove", "ChangeChase");
        ctx.AddPlayer(55, 50, 1, level: 30); // dist 5 — out of melee reach
        var near = ctx.AddPlayer(51, 50, 2, level: 30); // dist 1

        var svc = new MobChangeTargetService(ctx.Entities);
        var picked = svc.TryChangeChase(mob, range: 1);

        Assert.Same(near, picked);
    }

    // --- RANDOMTARGET (mob.cpp:1993 — single swing then re-aim at a random in-range enemy) ---

    [Fact]
    public void RandomTarget_swings_once_then_reaims()
    {
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 30, "Aggressive", "CanAttack", "CanMove", "RandomTarget");
        var a = ctx.AddPlayer(51, 50, 1, level: 30);
        var b = ctx.AddPlayer(52, 50, 2, level: 30);

        ctx.Ai.Tick(1000);

        // Single-swing path taken (continuous == false) and a valid in-range enemy was re-aimed.
        Assert.NotNull(mob.Attack);
        Assert.False(mob.Attack!.Continuous);
        Assert.Contains(mob.TargetId, new[] { a.Id.Value, b.Id.Value });
    }

    [Fact]
    public void Without_RandomTarget_attacks_continuously()
    {
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 30, "Aggressive", "CanAttack", "CanMove"); // no RandomTarget
        ctx.AddPlayer(51, 50, 1, level: 30);
        ctx.AddPlayer(52, 50, 2, level: 30);

        ctx.Ai.Tick(1000);

        Assert.NotNull(mob.Attack);
        Assert.True(mob.Attack!.Continuous);
    }

    [Fact]
    public void PickRandomEnemy_returns_an_in_range_enemy()
    {
        var ctx = Build();
        var mob = ctx.AddMob(50, 50, level: 30, "Aggressive", "CanAttack", "CanMove", "RandomTarget");
        var a = ctx.AddPlayer(51, 50, 1, level: 30);
        var b = ctx.AddPlayer(52, 50, 2, level: 30);

        var svc = new MobChangeTargetService(ctx.Entities);
        var picked = svc.PickRandomEnemy(mob, range: 3, new Random(0));

        Assert.True(picked == a || picked == b);
    }

    // --- proactive attacker switch (mob.cpp:1785-1851) ---

    [Fact]
    public void Attacker_switch_retargets_and_clears_attacked_id_when_gate_allows()
    {
        var ctx = Build();
        // Berserk state + ChangeTargetMelee → CanChangeTarget allows the switch.
        var mob = ctx.AddMob(50, 50, level: 30, "CanAttack", "CanMove", "ChangeTargetMelee");
        var a = ctx.AddPlayer(60, 50, 1, level: 30); // current target (far)
        var b = ctx.AddPlayer(70, 50, 2, level: 30); // rude attacker (far → out of melee range)
        mob.TargetId = a.Id.Value;
        mob.SkillState = MobSkillState.Berserk;

        // Cross the rude-attacked threshold so the escalation arm (which runs TrySetTarget) fires.
        for (var i = 0; i < RudeAttackedCondition.DefaultThreshold; i++)
            ctx.Ai.NotifyAttacked(mob, b);

        Assert.Equal(b.Id.Value, mob.TargetId); // switched to the attacker
        Assert.Equal(0, mob.AttackedId);              // cleared after the change-target decision
    }

    [Fact]
    public void Attacker_switch_blocked_when_gate_denies()
    {
        var ctx = Build();
        // Berserk state but NO ChangeTargetMelee → CanChangeTarget denies the switch.
        var mob = ctx.AddMob(50, 50, level: 30, "CanAttack", "CanMove");
        var a = ctx.AddPlayer(60, 50, 1, level: 30);
        var b = ctx.AddPlayer(70, 50, 2, level: 30);
        mob.TargetId = a.Id.Value;
        mob.SkillState = MobSkillState.Berserk;

        for (var i = 0; i < RudeAttackedCondition.DefaultThreshold; i++)
            ctx.Ai.NotifyAttacked(mob, b);

        Assert.Equal(a.Id.Value, mob.TargetId); // stayed on the original target
    }

    // --- scaffolding ---

    private static TestContext Build()
    {
        const string mapName = "ctmode_map";
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
        var sc = new StatusChangeService(damage, entities,
            new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);
        // Seeded rng → deterministic RANDOMTARGET re-aim.
        var ai = new MobAiService(entities, attack, NullLogger<MobAiService>.Instance,
            rng: new Random(0), movement: movement, sc: sc);
        return new TestContext(ai, entities, ids, (uint)mapName.GetHashCode(), sc);
    }

    private sealed record TestContext(
        MobAiService Ai,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId,
        StatusChangeService Sc)
    {
        public PlayerEntity AddPlayer(short x, short y, int charId, int level)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            pc.Level = level;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(short x, short y, int level, params string[] modes)
        {
            var modeMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in modes) modeMap[m] = true;
            var db = new MobDbEntry
            {
                Id = 1031, AegisName = "POPORING", Name = "Poporing",
                Hp = 500, ChaseRange = 12, AttackRange = 1, Level = level, Modes = modeMap,
            };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1031 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
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
