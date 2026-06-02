using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Acolyte;
using Map.Server.Skills.Behaviors.Swordman;
using Map.Server.Skills.Behaviors.Thief;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-14 — per-skill RE_LVL_DMOD divisors. A handful of weapon arms use
/// <c>RE_LVL_DMOD(120)</c>/<c>(150)</c> instead of the default 100
/// (battle.cpp). These plugins now override <c>ReLvlDivisor</c>, so above base
/// level 99 the ratio scales by <c>casterBaseLv / divisor</c> — e.g. at level
/// 300 a divisor-150 skill is ×2 and a divisor-120 skill is ×2.5 vs its
/// level-99 damage. (The remaining 120/150 skills on splash/plain bases + the
/// Ranger trap TMDMOD + the macro-omitting-arm audit are COMBAT-35.)
/// </summary>
public class Combat14ReLvlDivisorTests
{
    private const long Swing = 1000;

    private sealed class FixedSwingBattle : IBattleCalculator
    {
        public BattleDamage CalcWeaponAttack(Entity source, Entity target) => new() { Damage = Swing };
        public BattleDamage CalcMagicAttack(Entity s, Entity t, ushort id, ushort lv, int rate, long constant = 0) => new() { Damage = Swing };
        public BattleDamage CalcMiscAttack(Entity s, Entity t, ushort id, ushort lv, int rate) => new() { Damage = Swing };
    }

    private static (SkillExerciser ex, Map.Server.Skills.Behaviors.SkillBehaviorContext ctx) Fixed(int level)
    {
        var ex = new SkillExerciser(family: "Swordman");
        ex.Caster.Level = level;
        ex.Caster.Stats.Dex = 100;   // FeintBomb ratio uses DEX + job level.
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

    // divisor 150 → at lv300 the ratio portion is ×(300/150)=2 vs lv99.
    [Theory]
    [InlineData(typeof(PhantomThrust))]
    [InlineData(typeof(FallenEmpire))]
    public void Divisor150_doubles_ratio_at_level_300(System.Type t)
    {
        var d99 = Damage((WeaponSkillImpl)System.Activator.CreateInstance(t)!, 99, 5);
        var d300 = Damage((WeaponSkillImpl)System.Activator.CreateInstance(t)!, 300, 5);
        Assert.True(d99 > 0);
        Assert.Equal(d99 * 2, d300);            // 300/150 = 2
    }

    // divisor 120 → at lv300 the ratio portion is ×(300/120)=2.5 vs lv99.
    [Fact]
    public void FeintBomb_divisor120_scales_2point5x_at_level_300()
    {
        var d99 = Damage(new FeintBomb(), 99, 5);
        var d300 = Damage(new FeintBomb(), 300, 5);
        Assert.True(d99 > 0);
        // 2.5× expressed integer-safe: d300 == d99 * 300 / 120.
        Assert.Equal(d99 * 300 / 120, d300);
    }

    // Sanity: a default-divisor (100) skill scales ×3 at level 300, confirming
    // the overridden skills (×2 / ×2.5) genuinely differ from the default.
    [Fact]
    public void DefaultDivisor100_scales_3x_at_level_300()
    {
        // COMBAT-56 — Bash omits RE_LVL_DMOD; use RK_SONICWAVE (macro-using, default
        // divisor 100) to exercise the default-divisor scaling.
        var d99 = Damage(new SonicWave(), 99, 5);
        var d300 = Damage(new SonicWave(), 300, 5);
        Assert.True(d99 > 0);
        Assert.Equal(d99 * 3, d300);            // 300/100 = 3 (default divisor)
    }
}
