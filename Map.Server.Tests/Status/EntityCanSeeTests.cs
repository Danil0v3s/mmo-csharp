using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// Wave 81 — <see cref="EntityActionGates.CanSee"/> (rAthena
/// status_check_visibility, status.cpp:2292). Verifies the hide-set
/// + boss/detector matrix.
/// </summary>
public class EntityCanSeeTests
{
    private static MobEntity NewMob(int id, MobMode mode = MobMode.None)
    {
        var m = new MobEntity(new EntityId(id), classId: 1001,
            name: $"mob{id}", mapId: 0, x: 0, y: 0);
        m.Stats.Mode = mode;
        return m;
    }

    [Fact]
    public void CanSee_TrueWhenNoHideSc()
    {
        var src = NewMob(1);
        var tgt = NewMob(2);
        var sc = new FakeSc();
        Assert.True(src.CanSee(tgt, sc));
    }

    [Fact]
    public void CanSee_FalseWhenTargetHasHiding()
    {
        var src = NewMob(1);
        var tgt = NewMob(2);
        var sc = new FakeSc();
        sc.Add(tgt, StatusType.Hiding);
        Assert.False(src.CanSee(tgt, sc));
    }

    [Fact]
    public void CanSee_TrueWhenSourceIsBoss_AgainstBaseHide()
    {
        var src = NewMob(1, MobMode.Mvp); // boss class
        var tgt = NewMob(2);
        var sc = new FakeSc();
        sc.Add(tgt, StatusType.Cloaking);
        Assert.True(src.CanSee(tgt, sc));
    }

    [Fact]
    public void CanSee_TrueWhenSourceIsDetector_AgainstBaseHide()
    {
        var src = NewMob(1, MobMode.Detector);
        var tgt = NewMob(2);
        var sc = new FakeSc();
        sc.Add(tgt, StatusType.Camouflage);
        Assert.True(src.CanSee(tgt, sc));
    }

    [Fact]
    public void CanSee_FalseWhenTargetHasPerfectHide_EvenAgainstDetector()
    {
        // CloakingExceed / Newmoon should hide from detectors too.
        var src = NewMob(1, MobMode.Detector);
        var tgt = NewMob(2);
        var sc = new FakeSc();
        sc.Add(tgt, StatusType.Cloakingexceed);
        Assert.False(src.CanSee(tgt, sc));
    }

    [Fact]
    public void CanSee_TrueWhenSourceIsBoss_AgainstPerfectHide()
    {
        // Bosses pierce even perfect-hide.
        var src = NewMob(1, MobMode.Mvp);
        var tgt = NewMob(2);
        var sc = new FakeSc();
        sc.Add(tgt, StatusType.Newmoon);
        Assert.True(src.CanSee(tgt, sc));
    }

    [Fact]
    public void CanSee_FalseWhenTargetHasFeintBomb_AgainstNonDetector()
    {
        var src = NewMob(1);
        var tgt = NewMob(2);
        var sc = new FakeSc();
        sc.Add(tgt, StatusType.Feintbomb);
        Assert.False(src.CanSee(tgt, sc));
    }

    [Fact]
    public void CanSee_TrueWhenTargetHasFeintBomb_AgainstDetector()
    {
        var src = NewMob(1, MobMode.Detector);
        var tgt = NewMob(2);
        var sc = new FakeSc();
        sc.Add(tgt, StatusType.Feintbomb);
        Assert.True(src.CanSee(tgt, sc));
    }

    [Fact]
    public void CanSee_FalseForElementalVeil_AgainstRegular()
    {
        var src = NewMob(1);
        var tgt = NewMob(2);
        var sc = new FakeSc();
        sc.Add(tgt, StatusType.ElementalVeil);
        Assert.False(src.CanSee(tgt, sc));
    }

    [Fact]
    public void CanSee_TrueWhenScNull()
    {
        var src = NewMob(1);
        var tgt = NewMob(2);
        Assert.True(src.CanSee(tgt, sc: null));
    }

    // -- minimal IStatusChangeService stub --

    private sealed class FakeSc : IStatusChangeService
    {
        private readonly Dictionary<(int, StatusType), StatusChange> _active = new();
        public void Add(Entity target, StatusType type) =>
            _active[(target.Id.Value, type)] = new StatusChange { Type = type, Val1 = 1 };

        public StatusChange? Get(Entity target, StatusType type) =>
            _active.GetValueOrDefault((target.Id.Value, type));

        public StatusChange? Start(Entity target, StatusType type, int val1, int val2, int val3, int val4, int durationMs, Entity? source = null, long nowTick = long.MinValue) => null;
        public bool End(Entity target, StatusType type) => false;
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
