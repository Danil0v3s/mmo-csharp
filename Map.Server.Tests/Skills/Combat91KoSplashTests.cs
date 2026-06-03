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
/// COMBAT-91 — KO_HUUMARANKA (Swirling Petal) and KO_BAKURETSU (Kunai Explosion) recursive-splash
/// arms now deal nonzero per-victim damage by routing through the shared
/// <c>SkillImpl.ComputeSkillDamage</c> ratio pipeline: base ratio (with the partner-skill terms),
/// RE_LVL_DMOD, the post-dmod <c>+10*job_level</c> (KO_BAKURETSU, not scaled by the macro), and the
/// SC_KAGEMUSYA caster multiply. rAthena battle.cpp:5647 / 5663.
/// </summary>
public class Combat91KoSplashTests
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

    // ---- KO_HUUMARANKA: -100 + 150*lv + STR + NJ_HUUMA*100, RE_LVL_DMOD(100), ×KAGEMUSYA ----

    [Fact]
    public void Huumaranka_splash_victim_takes_nonzero_ratio_damage()
    {
        // lv99 (RE_LVL_DMOD no-op), Str 50, NJ_HUUMA unlearned:
        // ratio = 100 + (-100 + 150*5) + 50 + 0 = 800% → swing×8.
        var (ex, ctx) = Fixed(level: 99);
        new SwirlingPetal().CastendDamageId(ex.Caster, ex.Target, 5, ctx);
        Assert.Equal(Swing * 800 / 100, FirstDamage(ex)); // 8000 (was 0 before COMBAT-91)
    }

    [Fact]
    public void Huumaranka_adds_the_nj_huuma_partner_term()
    {
        // NJ_HUUMA lv10 → +10*100 = +1000% to the ratio: 800 + 1000 = 1800%.
        var (ex, ctx) = Fixed(level: 99);
        ex.Caster.LearnedSkills[SkillIds.NJ_HUUMA] = 10;
        new SwirlingPetal().CastendDamageId(ex.Caster, ex.Target, 5, ctx);
        Assert.Equal(Swing * 1800 / 100, FirstDamage(ex)); // 18000
    }

    [Fact]
    public void Huumaranka_kagemusya_multiplies_the_ratio_by_val2_percent()
    {
        var (exOff, ctxOff) = Fixed(level: 99);
        new SwirlingPetal().CastendDamageId(exOff.Caster, exOff.Target, 5, ctxOff);

        var (exOn, ctxOn) = Fixed(level: 99);
        ctxOn.Sc!.Start(exOn.Caster, StatusType.Kagemusya, val1: 1, val2: 20, val3: 2, val4: 0,
            durationMs: 60_000, exOn.Caster);
        new SwirlingPetal().CastendDamageId(exOn.Caster, exOn.Target, 5, ctxOn);

        Assert.Equal(Swing * 960 / 100, FirstDamage(exOn));                 // 800 ×1.2 = 960% → 9600
        Assert.Equal(FirstDamage(exOff) * (100 + 20) / 100, FirstDamage(exOn));
    }

    // ---- KO_BAKURETSU: -100 + TOBIDOUGU*(50+dex/4)*lv*4/10, RE_LVL_DMOD(120), +10*joblv, ×KAGEMUSYA ----

    [Fact]
    public void Bakuretsu_uses_the_real_tobidougu_factor_and_post_dmod_joblevel()
    {
        // lv99, Dex 50 (→ 50+12 = 62), JobLevel 70, NJ_TOBIDOUGU lv5:
        // base = 100 + (-100 + 5*62*5*4/10) = 620; + post-dmod 10*70 = 700 → 1320%.
        var (ex, ctx) = Fixed(level: 99);
        ex.Caster.LearnedSkills[SkillIds.NJ_TOBIDOUGU] = 5;
        new KunaiExplosion().CastendDamageId(ex.Caster, ex.Target, 5, ctx);
        Assert.Equal(Swing * 1320 / 100, FirstDamage(ex)); // 13200 (was 0 before COMBAT-91)
    }

    [Fact]
    public void Bakuretsu_tobidougu_factor_is_read_not_hardcoded_one()
    {
        // Unlearned NJ_TOBIDOUGU → factor 0 → the dex term vanishes; only the post-dmod +700 remains.
        var (ex, ctx) = Fixed(level: 99);
        new KunaiExplosion().CastendDamageId(ex.Caster, ex.Target, 5, ctx);
        Assert.Equal(Swing * 700 / 100, FirstDamage(ex)); // 7000 — distinct from the lv5 case (13200)
    }

    [Fact]
    public void Bakuretsu_joblevel_add_is_post_dmod_not_scaled_by_the_macro()
    {
        // lv150 → RE_LVL_DMOD(120) scales the BASE ratio (620 → 620*150/120 = 775), but the
        // +10*job_level (700) is added AFTER, unscaled: 775 + 700 = 1475% (NOT 1320*150/120 = 1650).
        var (ex, ctx) = Fixed(level: 150);
        ex.Caster.LearnedSkills[SkillIds.NJ_TOBIDOUGU] = 5;
        new KunaiExplosion().CastendDamageId(ex.Caster, ex.Target, 5, ctx);
        Assert.Equal(Swing * 1475 / 100, FirstDamage(ex)); // 14750
    }

    [Fact]
    public void Bakuretsu_kagemusya_multiplies_after_the_post_dmod_add()
    {
        var (exOff, ctxOff) = Fixed(level: 99);
        exOff.Caster.LearnedSkills[SkillIds.NJ_TOBIDOUGU] = 5;
        new KunaiExplosion().CastendDamageId(exOff.Caster, exOff.Target, 5, ctxOff);

        var (exOn, ctxOn) = Fixed(level: 99);
        exOn.Caster.LearnedSkills[SkillIds.NJ_TOBIDOUGU] = 5;
        ctxOn.Sc!.Start(exOn.Caster, StatusType.Kagemusya, val1: 1, val2: 20, val3: 2, val4: 0,
            durationMs: 60_000, exOn.Caster);
        new KunaiExplosion().CastendDamageId(exOn.Caster, exOn.Target, 5, ctxOn);

        Assert.Equal(Swing * 1584 / 100, FirstDamage(exOn));                // 1320 ×1.2 = 1584% → 15840
        Assert.Equal(FirstDamage(exOff) * (100 + 20) / 100, FirstDamage(exOn));
    }
}
