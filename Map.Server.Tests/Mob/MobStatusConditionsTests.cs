using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Mob.Conditions;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.9a — unit tests for the MSC_MYSTATUSON / MSC_MYSTATUSOFF
/// evaluators. rAthena <c>mob.cpp:4340</c> reads
/// <c>mob_getstatus(md, type)</c> against the mob's own status_change
/// list; we mirror that through <see cref="MobConditionContext.Sc"/>.
///
/// <para>Covers four shapes: direct SC match, direct SC miss, the
/// SC_NONE (cond2==0) wildcard that scans the common-SC block, and
/// the same wildcard's inverse. The cond2==0 path is the one rAthena
/// uses on the boss-template "buff if any common debuff present" rows
/// so it has to actually iterate the block.</para>
/// </summary>
public class MobStatusConditionsTests
{
    [Fact]
    public void MyStatusOn_DirectMatch_FiresWhenScActive()
    {
        var mob = MakeMob();
        var sc = new FakeSc();
        sc.Set(mob, StatusType.Poison);

        var ev = new MyStatusOnCondition();
        var entry = MakeEntry(MobSkillCondition.MyStatusOn, cond2: (int)StatusType.Poison);

        Assert.True(ev.IsMet(mob, entry, new MobConditionContext { Sc = sc }));
    }

    [Fact]
    public void MyStatusOn_DirectMatch_FailsWhenScMissing()
    {
        var mob = MakeMob();
        var sc = new FakeSc();
        // No SC applied.

        var ev = new MyStatusOnCondition();
        var entry = MakeEntry(MobSkillCondition.MyStatusOn, cond2: (int)StatusType.Poison);

        Assert.False(ev.IsMet(mob, entry, new MobConditionContext { Sc = sc }));
    }

    [Fact]
    public void MyStatusOn_Cond2Zero_FiresOnAnyCommonStatus()
    {
        // rAthena: cond2 == SC_NONE collapses to a sweep across
        // SC_COMMON_MIN..SC_COMMON_MAX. Stun is in that block.
        var mob = MakeMob();
        var sc = new FakeSc();
        sc.Set(mob, StatusType.Stun);

        var ev = new MyStatusOnCondition();
        var entry = MakeEntry(MobSkillCondition.MyStatusOn, cond2: 0);

        Assert.True(ev.IsMet(mob, entry, new MobConditionContext { Sc = sc }));
    }

    [Fact]
    public void MyStatusOff_Cond2Zero_FiresWhenNoCommonStatusPresent()
    {
        // Inverse wildcard: fires only when the mob is clean of every
        // common debuff. Empty fake → all-clear → match.
        var mob = MakeMob();
        var sc = new FakeSc();

        var ev = new MyStatusOffCondition();
        var entry = MakeEntry(MobSkillCondition.MyStatusOff, cond2: 0);

        Assert.True(ev.IsMet(mob, entry, new MobConditionContext { Sc = sc }));

        // Apply a common SC → wildcard inverse must flip to false.
        sc.Set(mob, StatusType.Poison);
        Assert.False(ev.IsMet(mob, entry, new MobConditionContext { Sc = sc }));
    }

    [Fact]
    public void MyStatusOn_NoScService_NoMatch()
    {
        // Defensive: if the context didn't supply Sc (legacy callers,
        // or a test bag that doesn't care), we must NOT throw and we
        // must NOT spuriously fire. Same shape as the Friend/Master
        // evaluators when ISlaveMobService is null.
        var mob = MakeMob();
        var ev = new MyStatusOnCondition();
        var entry = MakeEntry(MobSkillCondition.MyStatusOn, cond2: (int)StatusType.Poison);

        Assert.False(ev.IsMet(mob, entry, MobConditionContext.Empty));
    }

    // --- helpers ---

    private static MobEntity MakeMob()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 100 };
        var origin = new MobSpawnEntry { MapId = 1, MobClassId = 1002 };
        var mob = new MobEntity(new EntityId(1), db, origin, mapId: 1, x: 0, y: 0);
        mob.MaxHp = 100;
        mob.Hp = 100;
        return mob;
    }

    private static MobSkillEntry MakeEntry(MobSkillCondition cond, int cond2 = 0)
        => new()
        {
            SkillId = 1,
            SkillLevel = 1,
            State = MobSkillState.Any,
            Condition = cond,
            Cond2 = cond2,
        };

    /// <summary>
    /// Minimal IStatusChangeService for the evaluator tests. The full
    /// StatusChangeService needs damage + entity registry + effect
    /// registry plumbing; the evaluators only call Get, so a Dictionary
    /// keyed on (Entity.Id, type) is enough.
    /// </summary>
    private sealed class FakeSc : IStatusChangeService
    {
        private readonly Dictionary<(EntityId id, StatusType t), StatusChange> _active = new();

        public void Set(Entity target, StatusType type)
            => _active[(target.Id, type)] = new StatusChange { Type = type, ExpiresAt = -1 };

        public StatusChange? Get(Entity target, StatusType type)
            => _active.TryGetValue((target.Id, type), out var sc) ? sc : null;

        public StatusChange? Start(Entity target, StatusType type, int v1, int v2, int v3, int v4, int durationMs, Entity? source = null, long nowTick = long.MinValue)
        {
            var sc = new StatusChange { Type = type, Val1 = v1, Val2 = v2, Val3 = v3, Val4 = v4, ExpiresAt = -1 };
            _active[(target.Id, type)] = sc;
            return sc;
        }

        public bool End(Entity target, StatusType type) => _active.Remove((target.Id, type));

        public void Tick(long nowTick) { }

        // ST.1 no-op fakes.
        public int ClearAll(Entity target, byte type = 0) => 0;
        public int ClearBuffs(Entity target, SccbFlag flag) => 0;
        public int ClearOnChangeMap(Entity target) => 0;
        public int ClearOnLogout(Entity target) => 0;
        public int Spread(Entity source, Entity target) => 0;
        public int GetMaxStacks(StatusType type) => 1;
        public bool IsDisabledOnMap(uint mapId, StatusType type) => false;
        public int Refresh(Entity target) => 0;
    }
}
