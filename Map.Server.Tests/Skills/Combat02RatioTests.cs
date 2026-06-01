using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Acolyte;
using Map.Server.Skills.Behaviors.Archer;
using Map.Server.Skills.Behaviors.Swordman;
using Map.Server.Spawn;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-02 — per-skill damage ratio + the constant-addition stage. rAthena
/// order (battle.cpp:7708-7711): ATK_RATE(skillratio) then ATK_ADD(constant).
/// Uses a fixed-swing <see cref="IBattleCalculator"/> stub so the dealt damage
/// is exactly <c>swing × ratio / 100 + constant</c>.
/// </summary>
public class Combat02RatioTests
{
    private const long Swing = 1000;

    private sealed class FixedSwingBattle : IBattleCalculator
    {
        public BattleDamage CalcWeaponAttack(Entity source, Entity target)
            => new() { Damage = Swing };
        public BattleDamage CalcMagicAttack(Entity s, Entity t, ushort id, ushort lv, int rate, long constant = 0) => new() { Damage = Swing };
        public BattleDamage CalcMiscAttack(Entity s, Entity t, ushort id, ushort lv, int rate) => new() { Damage = Swing };
    }

    private static (SkillExerciser ex, Map.Server.Skills.Behaviors.SkillBehaviorContext ctx) Fixed()
    {
        var ex = new SkillExerciser(family: "Swordman");
        return (ex, ex.Context with { Battle = new FixedSwingBattle() });
    }

    private static long[] DamageEvents(SkillExerciser ex) =>
        ex.Recorder.Events.Where(e => e.Kind == "damage")
            .Select(e => (long)(int)e.Data["damage"]!).ToArray();

    // ---- ratio hooks (rAthena battle_calc_attack_skill_ratio) ----

    [Fact]
    public void Bash_ratio_is_100_plus_30_per_level()
    {
        var ex = new SkillExerciser();
        var bash = new Bash();
        // SM_BASH: skillratio += 30*lv  (battle.cpp:4640) → lv10 = 400%.
        Assert.Equal(400, bash.CalculateSkillRatio(100, ex.Caster, ex.Target, 10));
        Assert.Equal(130, bash.CalculateSkillRatio(100, ex.Caster, ex.Target, 1));
    }

    [Fact]
    public void Asura_ratio_includes_sp_term_and_constant_is_250_plus_150_per_level()
    {
        var (ex, ctx) = Fixed();
        ex.Caster.Sp = 1000;
        var asura = new AsuraStrike();
        // MO_EXTREMITYFIST: skillratio += 700 + sp*10 (battle.cpp:4843).
        Assert.Equal(100 + 700 + 1000 * 10, asura.CalculateSkillRatio(100, ex.Caster, ex.Target, 5));
        // constant addition: 250 + 150*lv (battle.cpp:6616).
        Assert.Equal(250 + 150 * 5, asura.CalculateSkillConstantAddition(ex.Caster, ex.Target, 5, ctx));
    }

    // ---- pipeline: ratio THEN constant, applied once ----

    [Fact]
    public void Asura_pipeline_applies_ratio_then_constant()
    {
        var (ex, ctx) = Fixed();
        ex.Caster.Sp = 500;
        const ushort lv = 5;
        var asura = new AsuraStrike();
        asura.CastendDamageId(ex.Caster, ex.Target, lv, ctx);

        var ratio = 100 + 700 + 500 * 10;        // 5800
        var expected = Swing * ratio / 100 + (250 + 150 * lv); // 58000 + 1000
        Assert.Equal(new[] { expected }, DamageEvents(ex));
    }

    [Fact]
    public void Bash_pipeline_has_no_constant_and_applies_ratio_once()
    {
        var (ex, ctx) = Fixed();
        var bash = new Bash();
        bash.CastendDamageId(ex.Caster, ex.Target, 10, ctx);
        // 1000 × 400% + 0 constant = 4000, applied exactly once.
        Assert.Equal(new[] { 4000L }, DamageEvents(ex));
    }

    [Fact]
    public void DoubleStrafe_deals_two_hits_at_90_plus_10_per_level()
    {
        var (ex, ctx) = Fixed();
        var ds = new DoubleStrafe();
        ds.CastendDamageId(ex.Caster, ex.Target, 10, ctx);
        // AC_DOUBLE: +10*(lv-1) → lv10 = 190%. Two hits.
        Assert.Equal(new[] { 1900L, 1900L }, DamageEvents(ex));
    }

    // ---- Magnum inner/outer rate (battle.cpp:4644) ----

    [Theory]
    [InlineData(1, 100 + 20 * 10)] // inner 3x3 (Chebyshev ≤1): 100+20*lv
    [InlineData(2, 100 + 10 * 10)] // outer 5x5 ring:           100+10*lv
    public void Magnum_inner_vs_outer_rate(int dist, int expectedRatePct)
    {
        var (ex, ctx) = Fixed();
        var magnum = new MagnumBreak();
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = ex.Caster.MapId, MobClassId = 1002 };
        var victim = new MobEntity(new EntityId(3000), db, origin, ex.Caster.MapId,
            (short)(ex.Caster.X + dist), ex.Caster.Y);
        var dmg = magnum.SplashDamage(ex.Caster, victim, 10, ctx);
        Assert.Equal(Swing * expectedRatePct / 100, dmg);
    }

    // ---- no double-count: a WeaponSkillImpl ratio path must not also route
    //      through the legacy DamageRate SkillAttack path ----

    [Fact]
    public void WeaponSkillImpl_does_not_also_invoke_DamageRate_SkillAttack()
    {
        var (ex, ctx) = Fixed();
        new Bash().CastendDamageId(ex.Caster, ex.Target, 5, ctx);
        Assert.DoesNotContain(ex.Recorder.Events, e => e.Kind == "skill-attack");
        Assert.Contains(ex.Recorder.Events, e => e.Kind == "damage");
    }
}
