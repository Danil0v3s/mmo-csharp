using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Ninja;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-75 — the SC_KAGEMUSYA (Shadow Warrior) caster ratio bonus
/// <c>skillratio += skillratio * val2 / 100</c> (val2 = 20) applied as the final ratio
/// step on the affected Ninja/Kagerou arms. Verified on KO_JYUMONJIKIRI (KoCrossSlash),
/// the single-target arm that flows through <c>ComputeSkillDamage</c>. rAthena battle.cpp:5644.
/// </summary>
public class Combat75KagemusyaTests
{
    private const long Swing = 1000;

    private sealed class FixedSwingBattle : IBattleCalculator
    {
        public BattleDamage CalcWeaponAttack(Entity s, Entity t) => new() { Damage = Swing };
        public BattleDamage CalcMagicAttack(Entity s, Entity t, ushort i, ushort l, int r, long c = 0) => new() { Damage = Swing };
        public BattleDamage CalcMiscAttack(Entity s, Entity t, ushort i, ushort l, int r) => new() { Damage = Swing };
    }

    private static (SkillExerciser ex, SkillBehaviorContext ctx) Fixed(int level)
    {
        var ex = new SkillExerciser(family: "Ninja");
        ex.Caster.Level = level;
        return (ex, ex.Context with { Battle = new FixedSwingBattle(), UnitOps = ex.UnitOps });
    }

    private static long FirstDamage(SkillExerciser ex) =>
        ex.Recorder.Events.Where(e => e.Kind == "damage")
            .Select(e => (long)(int)e.Data["damage"]!).First();

    [Fact]
    public void Kagemusya_boosts_jyumonjikiri_ratio_by_val2_percent()
    {
        // Caster level 99 → RE_LVL_DMOD(120) is a no-op. lv5 base ratio = -100 + 200*5 = 900
        // → +100 baseRatio = 1000%. No SC_JYUMONJIKIRI on a fresh target → no post-dmod add.
        var (exOff, ctxOff) = Fixed(level: 99);
        new KoCrossSlash().CastendDamageId(exOff.Caster, exOff.Target, 5, ctxOff);
        Assert.Equal(Swing * 1000 / 100, FirstDamage(exOff));               // 10000 (no SC)

        // With SC_KAGEMUSYA (val2 = 20) on the CASTER: 1000 + 1000*20/100 = 1200%.
        var (exOn, ctxOn) = Fixed(level: 99);
        ctxOn.Sc!.Start(exOn.Caster, StatusType.Kagemusya, val1: 1, val2: 20, val3: 2, val4: 0,
            durationMs: 60_000, exOn.Caster);
        new KoCrossSlash().CastendDamageId(exOn.Caster, exOn.Target, 5, ctxOn);
        Assert.Equal(Swing * 1200 / 100, FirstDamage(exOn));               // 12000 (×120%)

        // The boost is exactly val2% over the un-buffed hit.
        Assert.Equal(FirstDamage(exOff) * (100 + 20) / 100, FirstDamage(exOn));
    }

    [Fact]
    public void Kagemusya_multiplies_after_the_jyumonjikiri_post_dmod_add()
    {
        // rAthena order: base → RE_LVL_DMOD → +lv*srcLv (SC_JYUMONJIKIRI) → ×(100+val2)/100.
        // So the KAGEMUSYA multiply must scale the JYUMONJIKIRI-boosted ratio too.
        var (ex, ctx) = Fixed(level: 99);
        ctx.Sc!.Start(ex.Caster, StatusType.Kagemusya, val1: 1, val2: 20, val3: 2, val4: 0,
            durationMs: 60_000, ex.Caster);

        var sk = new KoCrossSlash();
        sk.CastendDamageId(ex.Caster, ex.Target, 5, ctx);   // cast 1: no JYUMONJIKIRI yet → 1000% ×1.2 = 1200%
        sk.CastendDamageId(ex.Caster, ex.Target, 5, ctx);   // cast 2: +5*99 = 1495% ×1.2 = 1794%

        var dmgs = ex.Recorder.Events.Where(e => e.Kind == "damage")
            .Select(e => (long)(int)e.Data["damage"]!).ToList();
        Assert.Equal(Swing * 1200 / 100, dmgs[0]);                          // 12000
        Assert.Equal(Swing * ((1000 + 5 * 99) * 120 / 100) / 100, dmgs[1]); // 1495 → ×1.2 = 1794% → 17940
    }

    [Fact]
    public void No_kagemusya_no_multiply()
    {
        var (ex, ctx) = Fixed(level: 99);
        new KoCrossSlash().CastendDamageId(ex.Caster, ex.Target, 5, ctx);
        Assert.Equal(Swing * 1000 / 100, FirstDamage(ex)); // unbuffed 1000%
    }
}
