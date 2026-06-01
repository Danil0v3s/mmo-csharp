using System.Linq;
using System.Reflection;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// SC-01 — the registry must be order-independent: a presence-only marker
/// (OnStart == shared <c>_NoOp</c>) must never overwrite a real OnStart body,
/// no matter which registers first. Complements
/// <c>StatusEffectCompletenessTests.Every_CalcFlag_SC_has_a_real_stat_mod_handler</c>
/// (which proves every CalcFlag SC mutates) by pinning the guard that keeps it
/// that way against future wave re-orders.
/// </summary>
public class StatusEffectShadowGuardTests
{
    private static readonly FieldInfo _noOpField =
        typeof(StatusEffectRegistry).GetField("_NoOp", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo _presenceMarker =
        typeof(StatusEffectRegistry).GetMethod("PresenceMarker", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static StatusEffectHandler Marker(ScfFlag flags)
        => (StatusEffectHandler)_presenceMarker.Invoke(null, new object[] { flags })!;

    private static StatusEffectHandler RealBody(ScfFlag flags) => new(
        OnStart: (e, sc, _) => e.Stats.Str = (short)(e.Stats.Str + sc.Val1),
        OnEnd: (e, sc) => e.Stats.Str = (short)(e.Stats.Str - sc.Val1),
        Flags: flags);

    // Mirrors StatusEffectCompletenessTests.MakeFreshMob: every stat non-zero
    // so a debuff handler (e.g. TinderBreaker: Flee -= …) shows a mutation
    // instead of clamping a 0 to 0.
    private static MobEntity FreshMob()
    {
        var mob = new MobEntity(new EntityId(1), 1002, "Poring", mapId: 0, x: 0, y: 0);
        var s = mob.Stats;
        s.Str = s.Agi = s.Vit = s.IntStat = s.Dex = s.Luk = 50;
        s.Pow = s.Sta = s.Wis = s.Spl = s.Con = s.Crt = 10;
        s.Hit = s.Flee = s.Cri = 100;
        s.Def = 50; s.Mdef = 25; s.Def2 = 30; s.Mdef2 = 20; s.Flee2 = 30;
        s.Hplus = 10; s.Crate = 10; s.Batk = 200; s.AspdRate = 50;
        s.Patk = 30; s.Smatk = 30; s.Res = 20; s.Mres = 20;
        s.MaxHp = 1000; s.Hp = 1000; s.MaxSp = 200; s.Sp = 200;
        return mob;
    }

    private static int Probe(StatusEffectHandler h, StatusType t)
    {
        var mob = FreshMob();
        var before = mob.Stats.Str;
        h.OnStart(mob, new StatusChange { Type = t, Val1 = 5 }, null);
        return mob.Stats.Str - before;
    }

    [Fact]
    public void PresenceMarker_uses_shared_NoOp_delegate()
    {
        var noOp = _noOpField.GetValue(null);
        var marker = Marker(ScfFlag.Buff);
        // Done-criterion 1: reference-equal to the shared _NoOp so the guard
        // and the RegisterDefaults NoOp-upgrade can detect it.
        Assert.Same(noOp, marker.OnStart);
    }

    [Fact]
    public void RealBody_survives_a_later_presence_marker_overwrite()
    {
        var reg = new StatusEffectRegistry();
        const StatusType t = StatusType.Provoke; // arbitrary; we overwrite it
        reg.Register(t, RealBody(ScfFlag.Buff));
        reg.Register(t, Marker(ScfFlag.RemoveOnDamaged)); // attempted downgrade
        var h = reg.Get(t)!;
        Assert.Equal(5, Probe(h, t));                       // body survived
        Assert.True(h.Flags.HasFlag(ScfFlag.Buff));         // original flag kept
        Assert.True(h.Flags.HasFlag(ScfFlag.RemoveOnDamaged)); // marker flag merged in
    }

    [Fact]
    public void PresenceMarker_then_real_body_keeps_the_real_body()
    {
        var reg = new StatusEffectRegistry();
        const StatusType t = StatusType.Provoke;
        reg.Register(t, Marker(ScfFlag.Buff));  // marker first
        reg.Register(t, RealBody(ScfFlag.Buff)); // real body second (normal wave order)
        Assert.Equal(5, Probe(reg.Get(t)!, t));
    }

    [Fact]
    public void Marker_over_marker_is_allowed()
    {
        var reg = new StatusEffectRegistry();
        const StatusType t = StatusType.Provoke;
        reg.Register(t, Marker(ScfFlag.Buff));
        reg.Register(t, Marker(ScfFlag.RemoveOnLogout));
        var h = reg.Get(t)!;
        Assert.Equal(0, Probe(h, t)); // still a no-op
        Assert.True(h.Flags.HasFlag(ScfFlag.RemoveOnLogout)); // last marker wins normally
    }

    [Theory]
    [InlineData(StatusType.Reflectdamage)]
    [InlineData(StatusType.Banding)]
    [InlineData(StatusType.Sunstance)]
    [InlineData(StatusType.Starstance)]
    [InlineData(StatusType.Inspiration)]
    [InlineData(StatusType.HeatBarrel)]
    [InlineData(StatusType.Bloodylust)]
    [InlineData(StatusType.Pyroclastic)]
    [InlineData(StatusType.Madogear)]
    [InlineData(StatusType.Moonlitserenade)]
    [InlineData(StatusType.TinderBreaker)]
    [InlineData(StatusType.HeaterOption)]
    public void FormerlyShadowed_CalcFlag_type_has_a_mutating_OnStart(StatusType t)
    {
        // These all had a PresenceMarker re-registration in the wave5 family
        // methods (now deleted). Their real body must be the live handler.
        var reg = new StatusEffectRegistry();
        var h = reg.Get(t);
        Assert.NotNull(h);
        var mob = FreshMob();
        int[] Snap() => new[]
        {
            mob.Stats.Str, mob.Stats.Agi, mob.Stats.Batk, mob.Stats.Def, mob.Stats.Def2,
            mob.Stats.Flee, mob.Stats.AspdRate, mob.Stats.MaxHp, mob.Stats.Cri, (int)mob.Stats.MatkMin,
        };
        var before = Snap();
        // A real body either mutates a listed stat or materializes a Val for a
        // combat consumer (Reflectdamage Val2 etc.); it must NOT be a pure no-op.
        var sc = new StatusChange { Type = t, Val1 = 5 };
        h!.OnStart(mob, sc, null);
        Assert.True(!Snap().SequenceEqual(before) || sc.Val2 != 0 || sc.Val3 != 0,
            $"{t} OnStart did nothing — its real body was lost (shadow regression).");
    }
}
