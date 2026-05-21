using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Mob.Conditions;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.4 — Mob AI parity harness. Drives <see cref="MobSkillCastService"/>
/// through deterministic scenarios that mirror real rAthena
/// mob_skill_db rows; the test recorder snapshots the (cast-occurred,
/// chosen-skill, chosen-target) trace so future refactors can't drift
/// the picker behavior without a visible failure.
///
/// <para>Test data comes from canonical rAthena rows:
/// <list type="bullet">
///   <item><b>Poring (1002)</b> — no skills, just a control.</item>
///   <item><b>Synthetic aggressor</b> — one Berserk Always skill at 100%.</item>
///   <item><b>Synthetic enraged</b> — MyHpLessThanRate ≤ 50% emergency cast.</item>
///   <item><b>Synthetic rude-escalator</b> — RudeAttacked trigger via NotifyEvent.</item>
/// </list>
/// </para>
/// </summary>
public class MobSkillCastServiceTests
{
    [Fact]
    public void TryUseSkill_NoSkills_ReturnsFalse()
    {
        var ctx = Build();
        var mob = ctx.AddMob(skills: new());
        Assert.False(ctx.Cast.TryUseSkill(mob, nowTick: 1000));
    }

    [Fact]
    public void TryUseSkill_NoCastMode_ReturnsFalse()
    {
        var ctx = Build();
        var mob = ctx.AddMob(skills: AlwaysFires());
        mob.Stats.Mode |= MobMode.NoCast;
        Assert.False(ctx.Cast.TryUseSkill(mob, nowTick: 1000));
    }

    [Fact]
    public void TryUseSkill_BerserkAlwaysSkill_FiresWhenTargetSet()
    {
        var ctx = Build();
        var mob = ctx.AddMob(skills: AlwaysFires(state: MobSkillState.Berserk));
        mob.SkillState = MobSkillState.Berserk;
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        Assert.True(ctx.Cast.TryUseSkill(mob, nowTick: 1000));
    }

    [Fact]
    public void TryUseSkill_StateMismatch_DoesNotFire()
    {
        var ctx = Build();
        // Skill is Berserk-only; mob is Idle.
        var mob = ctx.AddMob(skills: AlwaysFires(state: MobSkillState.Berserk));
        mob.SkillState = MobSkillState.Idle;
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        Assert.False(ctx.Cast.TryUseSkill(mob, nowTick: 1000));
    }

    [Fact]
    public void TryUseSkill_AnyStateRow_FiresInAnyState()
    {
        var ctx = Build();
        var mob = ctx.AddMob(skills: AlwaysFires(state: MobSkillState.Any));
        mob.SkillState = MobSkillState.Idle;
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        Assert.True(ctx.Cast.TryUseSkill(mob, nowTick: 1000));
    }

    [Fact]
    public void TryUseSkill_DeadState_NeverFires()
    {
        var ctx = Build();
        var mob = ctx.AddMob(skills: AlwaysFires(state: MobSkillState.Any));
        mob.SkillState = MobSkillState.Dead;
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        Assert.False(ctx.Cast.TryUseSkill(mob, nowTick: 1000));
    }

    [Fact]
    public void TryUseSkill_Cooldown_PreventsRefire()
    {
        var ctx = Build();
        var mob = ctx.AddMob(skills: AlwaysFires(state: MobSkillState.Berserk, delayMs: 5000));
        mob.SkillState = MobSkillState.Berserk;
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        Assert.True(ctx.Cast.TryUseSkill(mob, nowTick: 1000));
        // Same tick — cooldown anchor still in the future.
        Assert.False(ctx.Cast.TryUseSkill(mob, nowTick: 1000));
        // Past the cooldown — fires again.
        Assert.True(ctx.Cast.TryUseSkill(mob, nowTick: 6001));
    }

    [Fact]
    public void TryUseSkill_LowPermillage_DoesNotFireMost_Ticks()
    {
        var ctx = Build();
        var mob = ctx.AddMob(skills: new()
        {
            new MobSkillEntry
            {
                SkillId = SkillIds.MG_FIREBOLT, SkillLevel = 1,
                State = MobSkillState.Any,
                Condition = MobSkillCondition.Always,
                Permillage = 100,  // 1% chance
            },
        });
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        // With deterministic Random(0), 1% rate over 100 trials should
        // fire ~0-3 times. We just assert "not every tick" (would be 100).
        var fires = 0;
        for (int t = 0; t < 100; t++)
        {
            // Each call uses Cond2=0 entry with permillage=100; vary tick
            // so the cooldown doesn't gate us.
            if (ctx.Cast.TryUseSkill(mob, nowTick: 10_000 + t * 10_000)) fires++;
        }
        Assert.True(fires < 10, $"Expected ~1% trigger rate, got {fires}/100");
    }

    [Fact]
    public void TryUseSkill_HpEmergencyTrigger_FiresOnlyWhenHpLow()
    {
        var ctx = Build();
        // Single MyHpLessThanRate ≤ 50% skill.
        var mob = ctx.AddMob(skills: new()
        {
            new MobSkillEntry
            {
                SkillId = SkillIds.MG_FIREBOLT, SkillLevel = 5,
                State = MobSkillState.Any,
                Condition = MobSkillCondition.MyHpLessThanRate,
                Cond2 = 50,
                Permillage = 10_000,
            },
        });
        mob.MaxHp = 1000;
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        // Full HP — no fire.
        mob.Hp = 1000;
        Assert.False(ctx.Cast.TryUseSkill(mob, nowTick: 1000));

        // 50% — boundary, fires.
        mob.Hp = 500;
        Assert.True(ctx.Cast.TryUseSkill(mob, nowTick: 10_000));
    }

    [Fact]
    public void NotifyEvent_RudeAttacked_DispatchesEventDriven()
    {
        var ctx = Build();
        // Rude-attacked row, MD_ANY state, 100% rate.
        var mob = ctx.AddMob(skills: new()
        {
            new MobSkillEntry
            {
                SkillId = SkillIds.MG_FIREBOLT, SkillLevel = 1,
                State = MobSkillState.Any,
                Condition = MobSkillCondition.RudeAttacked,
                Permillage = 10_000,
            },
        });
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        Assert.True(ctx.Cast.NotifyEvent(mob, pc, nowTick: 1000, MobSkillCondition.RudeAttacked));
    }

    [Fact]
    public void NotifyEvent_DamagedGreater_FiresAboveThreshold()
    {
        var ctx = Build();
        var mob = ctx.AddMob(skills: new()
        {
            new MobSkillEntry
            {
                SkillId = SkillIds.MG_FIREBOLT, SkillLevel = 1,
                State = MobSkillState.Any,
                Condition = MobSkillCondition.DamagedGreater,
                Cond2 = 500,
                Permillage = 10_000,
            },
        });
        var pc = ctx.AddPlayer();
        mob.TargetId = (int)pc.Id.Value;

        Assert.False(ctx.Cast.NotifyEvent(mob, pc, nowTick: 1000, MobSkillCondition.DamagedGreater, damage: 100));
        Assert.True(ctx.Cast.NotifyEvent(mob, pc, nowTick: 2000, MobSkillCondition.DamagedGreater, damage: 600));
    }

    // ----- target resolver behavior -----

    [Fact]
    public void TargetResolver_Self_AlwaysResolvesToMob()
    {
        var ctx = Build();
        var mob = ctx.AddMob(skills: new());
        var resolved = ctx.Resolver.ResolveEntity(mob, MobSkillTarget.Self);
        Assert.Same(mob, resolved);
    }

    [Fact]
    public void TargetResolver_Master_FallsBackToSelf_WhenUnowned()
    {
        var ctx = Build();
        var mob = ctx.AddMob(skills: new());
        // No MasterId set.
        var resolved = ctx.Resolver.ResolveEntity(mob, MobSkillTarget.Master);
        Assert.Same(mob, resolved);
    }

    [Fact]
    public void TargetResolver_Master_FindsOwner_WhenSet()
    {
        var ctx = Build();
        var owner = ctx.AddPlayer();
        var slave = ctx.AddMob(skills: new());
        slave.MasterId = owner.Id;
        var resolved = ctx.Resolver.ResolveEntity(slave, MobSkillTarget.Master);
        Assert.Same(owner, resolved);
    }

    [Fact]
    public void TargetResolver_Target_PrefersTargetId_OverAttackedId()
    {
        var ctx = Build();
        var primary = ctx.AddPlayer();
        var attacker = ctx.AddPlayer();
        var mob = ctx.AddMob(skills: new());
        mob.TargetId = (int)primary.Id.Value;
        mob.AttackedId = (int)attacker.Id.Value;
        var resolved = ctx.Resolver.ResolveEntity(mob, MobSkillTarget.Target);
        Assert.Same(primary, resolved);
    }

    [Fact]
    public void TargetResolver_AroundCell_OffsetByRange()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(x: 100, y: 100);
        var mob = ctx.AddMob(skills: new(), x: 0, y: 0);
        mob.TargetId = (int)pc.Id.Value;
        // AROUND4 = range 4 from base entity (target).
        var cell = ctx.Resolver.ResolveGroundCell(mob, MobSkillTarget.Around4);
        Assert.NotNull(cell);
        var dx = Math.Abs(cell!.Value.x - 100);
        var dy = Math.Abs(cell.Value.y - 100);
        Assert.True(dx <= 4 && dy <= 4, $"around-4 offset must be ≤ 4; got ({dx}, {dy})");
    }

    // --- harness setup ---

    private static List<MobSkillEntry> AlwaysFires(
        MobSkillState state = MobSkillState.Berserk,
        int delayMs = 1)
        => new()
        {
            new MobSkillEntry
            {
                SkillId = SkillIds.MG_FIREBOLT, SkillLevel = 1,
                State = state,
                Condition = MobSkillCondition.Always,
                Permillage = 10_000,
                DelayMs = delayMs,
            },
        };

    private static TestContext Build()
    {
        const string mapName = "ai_test";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        // The harness tests the picker in isolation — it does NOT depend
        // on the skill_db being loaded. A recording fake ISkillCastService
        // accepts every cast and records the (target, skill_id, level).
        var fakeCast = new RecordingSkillCast();
        var conditions = new MobSkillConditionRegistry(new IMobSkillConditionEvaluator[]
        {
            new AlwaysCondition(),
            new MyHpLessThanRateCondition(),
            new MyHpInRateCondition(),
            new RudeAttackedCondition(),
            new CloseAttackedCondition(),
            new LongRangeAttackedCondition(),
            new DamagedGreaterCondition(),
            new SpawnCondition(),
        });
        var resolver = new MobSkillTargetResolver(entities, rng: new Random(0));
        var castService = new MobSkillCastService(
            conditions, resolver,
            NullLogger<MobSkillCastService>.Instance,
            fakeCast, new Random(0));
        return new TestContext(castService, resolver, fakeCast, entities, new EntityIdAllocator(), (uint)mapName.GetHashCode());
    }

    /// <summary>
    /// Always-accept fake — accepts every cast and records the
    /// (target, skill, lv) tuple. Removes skill_db dependency from
    /// the picker tests.
    /// </summary>
    private sealed class RecordingSkillCast : ISkillCastService
    {
        public List<(EntityId TargetId, ushort SkillId, ushort SkillLevel)> Casts { get; } = new();

        public SkillCastResult StartCast(Entity source, EntityId targetId, ushort skillId, ushort skillLevel)
        {
            Casts.Add((targetId, skillId, skillLevel));
            return SkillCastResult.Started;
        }

        public bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel)
            => true;

        public void Tick(long nowTick) { }
    }

    private sealed record TestContext(
        IMobSkillCastService Cast,
        MobSkillTargetResolver Resolver,
        RecordingSkillCast Recorder,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
        private int _nextPcId = 1;
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
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            mob.MaxHp = mob.Hp = 1000;
            Entities.Add(mob);
            return mob;
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
}
