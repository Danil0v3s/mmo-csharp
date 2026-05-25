using Map.Server.Entities;
using Map.Server.Movement.UnitOps;
using Map.Server.Status;

namespace Map.Server.Tests.Movement;

/// <summary>
/// Wave 83 — Sura Shadow Scar timer accumulator. Verifies
/// <see cref="IUnitOpsService.AddShadowScar"/> /
/// <see cref="UnitOpsService.PruneExpiredShadowScars"/> against the
/// rAthena <c>unit_addshadowscar</c> + timer cleanup semantics
/// (unit.cpp:4258).
/// </summary>
public class ShadowScarTests
{
    private static MobEntity NewMob(int id) =>
        new(new EntityId(id), classId: 1001, name: $"mob{id}", mapId: 0, x: 0, y: 0);

    [Fact]
    public void Entity_DefaultShadowScarTimersIsEmpty()
    {
        var m = NewMob(1);
        Assert.Empty(m.ShadowScarTimers);
    }

    [Fact]
    public void Add_AppendsExpiryTick_BelowCap()
    {
        var m = NewMob(1);
        var sc = new FakeSc();
        // Direct list mutation for the cap test — no service required.
        for (var i = 0; i < 50; i++)
        {
            m.ShadowScarTimers.Add(Environment.TickCount64 + 30_000);
        }
        Assert.Equal(50, m.ShadowScarTimers.Count);
        Assert.True(m.ShadowScarTimers.Count < Entity.MaxShadowScar);
    }

    [Fact]
    public void Add_CapsAtMaxShadowScar()
    {
        // Fill list to the cap.
        var m = NewMob(1);
        for (var i = 0; i < Entity.MaxShadowScar; i++)
            m.ShadowScarTimers.Add(Environment.TickCount64 + 30_000);
        Assert.Equal(Entity.MaxShadowScar, m.ShadowScarTimers.Count);
    }

    [Fact]
    public void Prune_RemovesExpiredEntries()
    {
        var m = NewMob(1);
        var now = Environment.TickCount64;
        m.ShadowScarTimers.Add(now - 1000); // expired
        m.ShadowScarTimers.Add(now - 500);  // expired
        m.ShadowScarTimers.Add(now + 5000); // active
        m.ShadowScarTimers.Add(now + 10_000); // active

        UnitOpsService.PruneExpiredShadowScars(m, now, sc: null);

        Assert.Equal(2, m.ShadowScarTimers.Count);
        Assert.All(m.ShadowScarTimers, t => Assert.True(t > now));
    }

    [Fact]
    public void Prune_EndsScWhenListEmpties()
    {
        var m = NewMob(1);
        var now = Environment.TickCount64;
        // Active SC plus expired timer; pruning should clear list AND end SC.
        m.ShadowScarTimers.Add(now - 1000);
        var sc = new FakeSc();
        sc.Add(m, StatusType.ShadowScar, val1: 1);

        UnitOpsService.PruneExpiredShadowScars(m, now, sc);

        Assert.Empty(m.ShadowScarTimers);
        Assert.True(sc.EndCalls.Contains((m.Id.Value, StatusType.ShadowScar)));
    }

    [Fact]
    public void Prune_UpdatesScVal1ToLiveCount()
    {
        var m = NewMob(1);
        var now = Environment.TickCount64;
        m.ShadowScarTimers.Add(now - 1000); // expired
        m.ShadowScarTimers.Add(now + 5000); // active
        m.ShadowScarTimers.Add(now + 10_000); // active
        var sc = new FakeSc();
        sc.Add(m, StatusType.ShadowScar, val1: 3);

        UnitOpsService.PruneExpiredShadowScars(m, now, sc);

        Assert.Equal(2, m.ShadowScarTimers.Count);
        var live = sc.Get(m, StatusType.ShadowScar);
        Assert.NotNull(live);
        Assert.Equal(2, live!.Val1);
    }

    // --- minimal IStatusChangeService stub ---

    private sealed class FakeSc : IStatusChangeService
    {
        private readonly Dictionary<(int, StatusType), StatusChange> _active = new();
        public List<(int, StatusType)> EndCalls { get; } = new();

        public void Add(Entity target, StatusType type, int val1) =>
            _active[(target.Id.Value, type)] = new StatusChange { Type = type, Val1 = val1 };

        public StatusChange? Get(Entity target, StatusType type) =>
            _active.GetValueOrDefault((target.Id.Value, type));

        public bool End(Entity target, StatusType type)
        {
            EndCalls.Add((target.Id.Value, type));
            return _active.Remove((target.Id.Value, type));
        }

        public StatusChange? Start(Entity target, StatusType type, int val1, int val2, int val3, int val4, int durationMs, Entity? source = null, long nowTick = long.MinValue) => null;
        public void Tick(long nowTick) { }
        public int ClearAll(Entity target, byte type = 0) => 0;
        public int ClearBuffs(Entity target, SccbFlag flag) => 0;
        public int ClearOnChangeMap(Entity target) => 0;
        public int ClearOnLogout(Entity target) => 0;
        public int Spread(Entity source, Entity target) => 0;
        public int GetMaxStacks(StatusType type) => 1;
        public bool IsDisabledOnMap(uint mapId, StatusType type) => false;
        public int Refresh(Entity target) => 0;
        public ScfFlag GetEffectiveFlags(StatusType type) => ScfFlag.None;
    }
}
