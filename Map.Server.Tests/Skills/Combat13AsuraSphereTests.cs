using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Acolyte;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-13 — MO_EXTREMITYFIST renewal ×2 ratio when the caster had MORE than
/// 5 spirit spheres at cast time (battle.cpp:4843-4847). The &gt;5 bit is
/// captured from <see cref="PlayerEntity.SpiritBall"/> in
/// <see cref="AsuraStrike.CastendDamageId"/> and threaded as the miscflag.
/// </summary>
public class Combat13AsuraSphereTests
{
    private const long Swing = 1000;

    private sealed class FixedSwingBattle : IBattleCalculator
    {
        public BattleDamage CalcWeaponAttack(Entity source, Entity target) => new() { Damage = Swing };
        public BattleDamage CalcMagicAttack(Entity s, Entity t, ushort id, ushort lv, int rate, long constant = 0) => new() { Damage = Swing };
        public BattleDamage CalcMiscAttack(Entity s, Entity t, ushort id, ushort lv, int rate) => new() { Damage = Swing };
    }

    private static (SkillExerciser ex, SkillBehaviorContext ctx) Fixed()
    {
        var ex = new SkillExerciser(family: "Acolyte");
        return (ex, ex.Context with { Battle = new FixedSwingBattle() });
    }

    private static long[] DamageEvents(SkillExerciser ex) =>
        ex.Recorder.Events.Where(e => e.Kind == "damage")
            .Select(e => (long)(int)e.Data["damage"]!).ToArray();

    // ---- ratio formula: ×2 only when miscflag bit 1 set ----

    [Fact]
    public void Ratio_doubles_when_miscflag_marks_more_than_5_spheres()
    {
        var (ex, ctx) = Fixed();
        ex.Caster.Sp = 100;
        var asura = new AsuraStrike();

        // No-ctx path: no >5 flag → 100 + 700 + 100*10 = 1800.
        var r5 = asura.CalculateSkillRatio(100, ex.Caster, ex.Target, 5);
        Assert.Equal(100 + 700 + 100 * 10, r5);

        // miscflag & 1 set → the whole ratio doubles (before the cap).
        var r6 = asura.CalculateSkillRatio(100, ex.Caster, ex.Target, 5, ctx, miscflag: 1);
        Assert.Equal(2 * r5, r6);

        // miscflag without bit 1 → no doubling.
        Assert.Equal(r5, asura.CalculateSkillRatio(100, ex.Caster, ex.Target, 5, ctx, miscflag: 0));
    }

    [Fact]
    public void Ratio_caps_at_500000_after_the_double()
    {
        var (ex, ctx) = Fixed();
        ex.Caster.Sp = 100_000; // 100 + 700 + 1_000_000 = 1_000_800; ×2 → capped.
        var asura = new AsuraStrike();
        Assert.Equal(500_000, asura.CalculateSkillRatio(100, ex.Caster, ex.Target, 5, ctx, miscflag: 1));
    }

    // ---- end-to-end: CastendDamageId reads SpiritBall and applies ×2 ----

    [Fact]
    public void CastendDamageId_with_6_spheres_doubles_the_ratio_portion()
    {
        const ushort lv = 5;
        const long constant = 250 + 150 * lv;

        // 6 spheres → >5 → ratio doubles.
        var (ex6, ctx6) = Fixed();
        ex6.Caster.Sp = 500;
        ex6.Caster.SpiritBall = 6;
        new AsuraStrike().CastendDamageId(ex6.Caster, ex6.Target, lv, ctx6);
        var dmg6 = DamageEvents(ex6).Single();

        // 5 spheres → no double.
        var (ex5, ctx5) = Fixed();
        ex5.Caster.Sp = 500;
        ex5.Caster.SpiritBall = 5;
        new AsuraStrike().CastendDamageId(ex5.Caster, ex5.Target, lv, ctx5);
        var dmg5 = DamageEvents(ex5).Single();

        var ratio = 100 + 700 + 500 * 10;                 // 5800
        Assert.Equal(Swing * ratio / 100 + constant, dmg5);          // 59000
        Assert.Equal(Swing * (ratio * 2) / 100 + constant, dmg6);    // 117000
        // The constant is added once on both; only the ratio portion doubles.
        Assert.Equal(2 * (dmg5 - constant), dmg6 - constant);
    }

    [Fact]
    public void CastendDamageId_with_exactly_5_spheres_does_not_double()
    {
        const ushort lv = 5;
        var (ex, ctx) = Fixed();
        ex.Caster.Sp = 500;
        ex.Caster.SpiritBall = 5; // boundary: 5 is NOT > 5.
        new AsuraStrike().CastendDamageId(ex.Caster, ex.Target, lv, ctx);

        var ratio = 100 + 700 + 500 * 10;
        Assert.Equal(new[] { Swing * ratio / 100 + (250 + 150 * lv) }, DamageEvents(ex));
    }
}
