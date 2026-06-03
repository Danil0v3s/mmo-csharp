using System;
using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Acolyte;
using Map.Server.Skills.Behaviors.Archer;
using Map.Server.Skills.Behaviors.Swordman;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-96 — the weapon plugins that compute <c>swing × ratio</c> directly (bypassing
/// <c>ComputeSkillDamage</c>) now build a skill-aware swing and apply the ÷200 skill crit_atk_rate
/// bump (battle.cpp:7787), not the auto-attack ÷100. Verified by the relationship
/// <c>withCar == noCar + noCar*car/200</c> on a critical swing (which is ≠ the ÷100 value).
/// </summary>
public class Combat96BypassCritTests
{
    private const int Car = 50;

    [Fact]
    public void Arrow_shower_critical_uses_over_200() => AssertSplash(new ArrowShower(), lvl: 5);

    [Fact]
    public void Magnum_break_critical_uses_over_200() => AssertSplash(new MagnumBreak(), lvl: 5);

    [Fact]
    public void Double_strafe_critical_uses_over_200() => AssertInline(() => new DoubleStrafe(), lvl: 5);

    [Fact]
    public void Chain_crush_combo_critical_uses_over_200() => AssertInline(() => new ChainCrushCombo(), lvl: 5);

    [Fact]
    public void Earth_shaker_critical_uses_over_200()
        => AssertInline(() => new EarthShaker(), lvl: 5, hideTarget: true); // hidden branch is the bypass

    // ---- splash plugins (SplashDamage returns the per-victim damage) ----

    private static void AssertSplash(RecursiveDamageSplashSkillImpl plugin, ushort lvl)
    {
        var noCar = SplashDamage(plugin, lvl, car: 0);
        var withCar = SplashDamage(plugin, lvl, car: Car);
        Assert.True(noCar > 0);
        Assert.Equal(noCar + noCar * Car / 200, withCar);   // ÷200 skill bump
        Assert.NotEqual(noCar + noCar * Car / 100, withCar); // NOT the auto-attack ÷100
    }

    private static long SplashDamage(RecursiveDamageSplashSkillImpl plugin, ushort lvl, int car)
    {
        var (ex, ctx) = Crit(car);
        return plugin.SplashDamage(ex.Caster, ex.Target, lvl, ctx);
    }

    // ---- inline plugins (apply via ctx.Damage → read the recorded amount) ----

    private static void AssertInline(Func<SkillImpl> make, ushort lvl, bool hideTarget = false)
    {
        var noCar = InlineDamage(make(), lvl, car: 0, hideTarget);
        var withCar = InlineDamage(make(), lvl, car: Car, hideTarget);
        Assert.True(noCar > 0);
        Assert.Equal(noCar + noCar * Car / 200, withCar);
        Assert.NotEqual(noCar + noCar * Car / 100, withCar);
    }

    private static long InlineDamage(SkillImpl plugin, ushort lvl, int car, bool hideTarget)
    {
        var (ex, ctx) = Crit(car);
        if (hideTarget) ctx.Sc!.Start(ex.Target, StatusType.Hiding, val1: 1, 0, 0, 0, durationMs: 60_000, ex.Target);
        plugin.CastendDamageId(ex.Caster, ex.Target, lvl, ctx);
        return ex.Recorder.Events.Where(e => e.Kind == "damage")
            .Select(e => (long)(int)e.Data["damage"]!).First();
    }

    // ---- harness ----

    private static (SkillExerciser ex, SkillBehaviorContext ctx) Crit(int car)
    {
        var ex = new SkillExerciser(family: "Combat");
        // Guaranteed critical swing: high Cri vs Luk 0 with the zero RNG roll.
        ex.Caster.Stats.WatkMin = ex.Caster.Stats.WatkMax = 100;
        ex.Caster.Stats.Batk = 0; ex.Caster.Stats.Cri = 1000; ex.Caster.Stats.Hit = 10000;
        ex.Caster.EquipBonuses.CritAtkRate = car;
        ex.Target.Stats.Def = 0; ex.Target.Stats.Def2 = 0; ex.Target.Stats.Res = 0;
        ex.Target.Stats.Flee = 0; ex.Target.Stats.Flee2 = 0; ex.Target.Stats.Luk = 0;
        ex.Target.Stats.DefenseElement = BattleElement.Neutral; ex.Target.Stats.ElementLevel = 1;
        var ctx = ex.Context with { Battle = new BattleCalculator(new ZeroRandom()) };
        return (ex, ctx);
    }

    private sealed class ZeroRandom : Random
    {
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => minValue;
    }
}
