using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Ninja;
using Map.Server.Skills.Behaviors.Swordman;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-35 — the two WeaponSkillImpl arms that route through the live
/// ComputeSkillDamage divisor path: LG_PINPOINTATTACK and KO_JYUMONJIKIRI both
/// use RE_LVL_DMOD(120) (battle.cpp:5401 / 5641). Above base level 99 the ratio
/// scales by <c>casterBaseLv / 120</c>; at level 240 that is exactly ×2.
///
/// (The remaining 120/150 arms live on splash / plain-SkillImpl bases whose
/// CalculateSkillRatio is not consumed by the damage funnel today — those, the
/// Ranger trap TMDMOD, and the macro-omitting-arm audit are COMBAT-54/55/56.)
/// </summary>
public class Combat35ReLvlDivisorTests
{
    private const long Swing = 1000;

    private sealed class FixedSwingBattle : IBattleCalculator
    {
        public BattleDamage CalcWeaponAttack(Entity source, Entity target) => new() { Damage = Swing };
        public BattleDamage CalcMagicAttack(Entity s, Entity t, ushort id, ushort lv, int rate, long constant = 0) => new() { Damage = Swing };
        public BattleDamage CalcMiscAttack(Entity s, Entity t, ushort id, ushort lv, int rate) => new() { Damage = Swing };
    }

    private static (SkillExerciser ex, SkillBehaviorContext ctx) Fixed(int level)
    {
        var ex = new SkillExerciser(family: "Swordman");
        ex.Caster.Level = level;
        ex.Caster.Stats.Agi = 50;   // PinpointAttack ratio uses AGI (level-independent).
        ex.Caster.JobLevel = 50;
        return (ex, ex.Context with { Battle = new FixedSwingBattle() });
    }

    private static long Damage(WeaponSkillImpl plugin, int level, ushort skillLv)
    {
        var (ex, ctx) = Fixed(level);
        plugin.CastendDamageId(ex.Caster, ex.Target, skillLv, ctx);
        return ex.Recorder.Events.Where(e => e.Kind == "damage")
            .Select(e => (long)(int)e.Data["damage"]!).First();
    }

    // divisor 120 → at lv240 the ratio portion is exactly ×(240/120)=2 vs lv99
    // (240/120 is integral, so no truncation creeps into the assertion).
    [Theory]
    [InlineData(typeof(PinpointAttack))]
    [InlineData(typeof(KoCrossSlash))]
    public void Divisor120_doubles_ratio_at_level_240(System.Type t)
    {
        var d99 = Damage((WeaponSkillImpl)System.Activator.CreateInstance(t)!, 99, 5);
        var d240 = Damage((WeaponSkillImpl)System.Activator.CreateInstance(t)!, 240, 5);
        Assert.True(d99 > 0);
        Assert.Equal(d99 * 2, d240);
    }

    // At/below level 99 the divisor does not engage (rAthena `lv > 99` guard).
    [Theory]
    [InlineData(typeof(PinpointAttack))]
    [InlineData(typeof(KoCrossSlash))]
    public void No_scaling_at_or_below_level_99(System.Type t)
    {
        var d50 = Damage((WeaponSkillImpl)System.Activator.CreateInstance(t)!, 50, 5);
        var d99 = Damage((WeaponSkillImpl)System.Activator.CreateInstance(t)!, 99, 5);
        Assert.Equal(d50, d99);
    }

    // The divisor-120 plugins scale slower than a default-divisor-100 skill at the
    // same level (300/100 = 3 vs 300/120 = 2.5), confirming the override engages.
    [Fact]
    public void Divisor120_scales_slower_than_default_100()
    {
        var pinpoint99 = Damage(new PinpointAttack(), 99, 5);
        var pinpoint300 = Damage(new PinpointAttack(), 300, 5);
        Assert.Equal(pinpoint99 * 300 / 120, pinpoint300);   // ×2.5, not ×3
    }
}
