using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Mob.Conditions;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.5 — rAthena mob_skill_db sweep. Loads canonical rows from
/// <c>rathena-fork/db/re/mob_skill_db.txt</c> into the picker and
/// verifies the right skills fire under the right conditions. The
/// rows here are copied verbatim from rAthena so any picker drift
/// surfaces as a test failure.
///
/// <para>Rows tested (mob_skill_db.txt format:
/// <c>MobID, Info, State, SkillID, Lv, Rate, CastTime, Delay,
///   Cancelable, Target, Condition, ConditionValue, val1..val5,
///   Emotion, Chat</c>):</para>
/// <list type="bullet">
///   <item>Poring 1002: 2 idle rows (loot+self emote, attack+target water)</item>
///   <item>Eddga 1115: rude-attacked teleport, low-hp powerup, chase fireball</item>
/// </list>
/// </summary>
public class RathenaMobSkillSweepTests
{
    /// <summary>
    /// Poring 1002 — attack state, target current target, always fires
    /// (permillage = 2000 = 20%). With deterministic Random(seed=0)
    /// we expect at least a couple of fires in a 30-tick window.
    /// </summary>
    [Fact]
    public void Poring_NpcWaterAttack_AttackState_TargetCurrent_FiresAtRate()
    {
        var ctx = Build();
        // 1002, Poring, attack, NPC_WATERATTACK (184), lv1, 2000 rate,
        // 0 casttime, 5000 delay, yes cancelable, target, always
        var mob = ctx.AddMob(skills: new()
        {
            new MobSkillEntry
            {
                SkillId = 184,  // NPC_WATERATTACK
                SkillLevel = 1,
                State = MobSkillState.Berserk, // attack maps to Berserk
                Permillage = 2_000,
                CastTimeMs = 0,
                DelayMs = 5_000,
                Cancelable = true,
                Target = MobSkillTarget.Target,
                Condition = MobSkillCondition.Always,
                Cond2 = 0,
            },
        });
        mob.SkillState = MobSkillState.Berserk;
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        // 30 ticks spaced past the 5000ms cooldown — every other tick
        // can re-roll. At 20% rate, expect at least one hit; the
        // important thing is "fires sometimes, not every tick."
        var fires = 0;
        for (int t = 0; t < 30; t++)
        {
            if (ctx.Cast.TryUseSkill(mob, nowTick: 100_000 + t * 10_000)) fires++;
        }
        Assert.InRange(fires, 1, 15);  // ~6 expected at 20% over 30 trials
    }

    /// <summary>
    /// Eddga 1115 — rude-attacked teleport, 100% rate. Should fire
    /// every time NotifyEvent(RudeAttacked) is called past cooldown.
    /// </summary>
    [Fact]
    public void Eddga_AlTeleport_RudeAttacked_FiresOnEscalation()
    {
        var ctx = Build();
        // 1115, Eddga, idle, AL_TELEPORT (26), lv1, 10000 rate,
        // 0 casttime, 0 delay, yes cancelable, self, rudeattacked
        var mob = ctx.AddMob(skills: new()
        {
            new MobSkillEntry
            {
                SkillId = 26, // AL_TELEPORT
                SkillLevel = 1,
                State = MobSkillState.Idle,
                Permillage = 10_000,  // 100%
                DelayMs = 0,
                Target = MobSkillTarget.Self,
                Condition = MobSkillCondition.RudeAttacked,
            },
        });
        mob.SkillState = MobSkillState.Idle;
        var pc = ctx.AddPlayer();

        // RudeAttacked event with no target dependency — fires on self.
        Assert.True(ctx.Cast.NotifyEvent(mob, pc, nowTick: 1000, MobSkillCondition.RudeAttacked));
    }

    /// <summary>
    /// Eddga 1115 — NPC_POWERUP at &lt; 30% HP, 100% rate, attack state.
    /// Must NOT fire at full HP, MUST fire when HP drops below 30%.
    /// </summary>
    [Fact]
    public void Eddga_NpcPowerUp_LowHpEmergency_FiresOnlyBelow30Pct()
    {
        var ctx = Build();
        // 1115, Eddga, attack, NPC_POWERUP (349), lv5, 10000 rate,
        // 0 casttime, 30000 delay, yes cancelable, self, myhpltmaxrate, 30
        var mob = ctx.AddMob(skills: new()
        {
            new MobSkillEntry
            {
                SkillId = 349, // NPC_POWERUP
                SkillLevel = 5,
                State = MobSkillState.Berserk,
                Permillage = 10_000,
                DelayMs = 30_000,
                Target = MobSkillTarget.Self,
                Condition = MobSkillCondition.MyHpLessThanRate,
                Cond2 = 30,
                Emotion = 6,
            },
        });
        mob.SkillState = MobSkillState.Berserk;
        mob.MaxHp = 1000;
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        // Full HP — doesn't fire.
        mob.Hp = 1000;
        Assert.False(ctx.Cast.TryUseSkill(mob, nowTick: 1000));

        // 50% HP — still doesn't fire (threshold = 30%).
        mob.Hp = 500;
        Assert.False(ctx.Cast.TryUseSkill(mob, nowTick: 10_000));

        // 25% HP — under threshold, fires.
        mob.Hp = 250;
        Assert.True(ctx.Cast.TryUseSkill(mob, nowTick: 20_000));
    }

    /// <summary>
    /// Eddga 1115 — chase-state MG_FIREBALL via SKILLUSED trigger (player
    /// just cast skill id 18 — MG_FIREBALL — in range, Eddga retaliates).
    /// </summary>
    [Fact]
    public void Eddga_MgFireball_SkillUsedReaction_FiresWhenMatchingSkillSeen()
    {
        var ctx = Build();
        // 1115, Eddga, chase, MG_FIREBALL (17), lv43, 10000 rate,
        // 0 casttime, 0 delay, yes cancelable, target, skillused, 18
        var mob = ctx.AddMob(skills: new()
        {
            new MobSkillEntry
            {
                SkillId = 17, // MG_FIREBALL
                SkillLevel = 43,
                State = MobSkillState.Any,  // chase maps to AnyTarget; Any subsumes it
                Permillage = 10_000,
                DelayMs = 0,
                Target = MobSkillTarget.Target,
                Condition = MobSkillCondition.SkillUsed,
                Cond2 = 18, // SkillUsed: MG_FIREBOLT (id 18)
            },
        });
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        // Not the matching skill — doesn't fire.
        Assert.False(ctx.Cast.NotifyEvent(mob, pc, nowTick: 1000,
            MobSkillCondition.SkillUsed, triggerSkillId: 999));

        // Matching skill (MG_FIREBOLT id 18) just used — fires.
        Assert.True(ctx.Cast.NotifyEvent(mob, pc, nowTick: 2000,
            MobSkillCondition.SkillUsed, triggerSkillId: 18));
    }

    // ----- harness setup (shared with MobSkillCastServiceTests) -----

    private static TestContext Build()
    {
        const string mapName = "sweep_test";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var fakeCast = new RecordingSkillCast();
        var conditions = new MobSkillConditionRegistry(new IMobSkillConditionEvaluator[]
        {
            new AlwaysCondition(),
            new MyHpLessThanRateCondition(),
            new MyHpInRateCondition(),
            new RudeAttackedCondition(),
            new SkillUsedCondition(),
        });
        var resolver = new MobSkillTargetResolver(entities, rng: new Random(0));
        var castService = new MobSkillCastService(
            conditions, resolver,
            NullLogger<MobSkillCastService>.Instance,
            fakeCast, new Random(0));
        return new TestContext(castService, fakeCast, entities, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        IMobSkillCastService Cast,
        RecordingSkillCast Recorder,
        EntityRegistry Entities,
        uint MapId)
    {
        private int _nextPcId = 1;
        private readonly EntityIdAllocator _ids = new();

        public PlayerEntity AddPlayer(short x = 100, short y = 100)
        {
            var charId = _nextPcId++;
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(List<MobSkillEntry> skills, short x = 50, short y = 50)
        {
            var db = new MobDbEntry
            {
                Id = 1002, AegisName = "TEST_MOB", Name = "Test", Hp = 1000,
                Skills = skills,
            };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(_ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            mob.MaxHp = mob.Hp = 1000;
            Entities.Add(mob);
            return mob;
        }
    }

    private sealed class RecordingSkillCast : ISkillCastService
    {
        public List<(EntityId TargetId, ushort SkillId, ushort SkillLevel)> Casts { get; } = new();
        public SkillCastResult StartCast(Entity source, EntityId targetId, ushort skillId, ushort skillLevel)
        {
            Casts.Add((targetId, skillId, skillLevel));
            return SkillCastResult.Started;
        }
        public bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel) => true;
        public void Tick(long nowTick) { }
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
}
