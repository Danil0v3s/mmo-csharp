using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Ninja;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-57 — KO_JYUMONJIKIRI: the <c>+lv*srcBaseLv</c> ratio bonus when the target
/// already carries SC_JYUMONJIKIRI (added AFTER RE_LVL_DMOD), the two-hit div, and
/// the caster position-shift before striking. rAthena battle.cpp:5639 / skill.cpp:7128.
/// </summary>
public class Combat57KoCrossSlashTests
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

    [Fact]
    public void Target_with_sc_jyumonjikiri_takes_the_boosted_ratio()
    {
        // Caster level 99 → RE_LVL_DMOD(120) is a no-op, so the math is clean.
        // lv5 base ratio = -100 + 200*5 = 900 → +100 baseRatio = 1000% → 10000.
        // Cast 1 applies SC_JYUMONJIKIRI; cast 2 reads it → +lv*srcLv = 5*99 = 495%.
        var (ex, ctx) = Fixed(level: 99);
        var sk = new KoCrossSlash();

        sk.CastendDamageId(ex.Caster, ex.Target, 5, ctx);   // no SC yet → 1000%
        sk.CastendDamageId(ex.Caster, ex.Target, 5, ctx);   // SC present → 1495%

        var dmgs = ex.Recorder.Events.Where(e => e.Kind == "damage")
            .Select(e => (long)(int)e.Data["damage"]!).ToList();
        Assert.Equal(2, dmgs.Count);
        Assert.Equal(Swing * 1000 / 100, dmgs[0]);                 // 10000
        Assert.Equal(Swing * (1000 + 5 * 99) / 100, dmgs[1]);      // 14950
    }

    [Fact]
    public void Hit_count_is_two()
        => Assert.Equal(2, new KoCrossSlash().GetMultiHitCount(5));

    [Fact]
    public void Position_shift_slides_the_caster_before_striking()
    {
        var (ex, ctx) = Fixed(level: 99);
        ex.Caster.X = 50; ex.Caster.Y = 50;
        ex.Target.X = 50; ex.Target.Y = 53; // target due north

        new KoCrossSlash().CastendDamageId(ex.Caster, ex.Target, 5, ctx);

        // dir = N (0) → x=0, y=-2 → caster slides to (target.x, target.y-2) = (50, 51).
        var move = ex.Recorder.Events.FirstOrDefault(e => e.Kind == "move-pos");
        Assert.NotNull(move);
        Assert.Equal(50, System.Convert.ToInt32(move!.Data["x"]));
        Assert.Equal(51, System.Convert.ToInt32(move.Data["y"]));
        // …and the strike still lands (damage recorded).
        Assert.Contains(ex.Recorder.Events, e => e.Kind == "damage");
    }
}
