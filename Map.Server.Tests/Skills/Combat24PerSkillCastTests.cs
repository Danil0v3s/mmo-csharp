using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Skills;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-24 — per-skill cast/delay tables + SA_ABRACADABRA. rAthena
/// skill_vfcastfix per-skill loops (skill.cpp:20357) + skill_delayfix abra
/// 0-delay (skill.cpp:20460).
/// </summary>
public class Combat24PerSkillCastTests
{
    // ---- per-skill cast applies only to the keyed skill ----

    [Fact]
    public void Per_skill_castrate_halves_only_the_named_skill()
    {
        var b = new EquipBonusBundle();
        // bonus2 bVariableCastrate,WZ_STORMGUST,50 → stored inversed (-50).
        BonusScriptExtractor.Apply("bonus2 bVariableCastrate,WZ_STORMGUST,50;", b);

        // Storm Gust: 1000ms variable → ×(100-50)/100 = 500.
        Assert.Equal((500, 0), SkillCastTimingService.ApplyPerSkillCast(1000, 0, SkillIds.WZ_STORMGUST, b));
        // A different skill is untouched.
        Assert.Equal((1000, 0), SkillCastTimingService.ApplyPerSkillCast(1000, 0, SkillIds.MG_FIREBOLT, b));
    }

    [Fact]
    public void Per_skill_flat_cast_adds_raw_ms()
    {
        var b = new EquipBonusBundle();
        // bonus2 bSkillVariableCast,WZ_METEOR,-200 → -200ms variable (faster);
        // bonus2 bSkillFixedCast,WZ_METEOR,-100 → -100ms fixed.
        BonusScriptExtractor.Apply("bonus2 bSkillVariableCast,WZ_METEOR,-200; bonus2 bSkillFixedCast,WZ_METEOR,-100;", b);

        Assert.Equal((800, 400), SkillCastTimingService.ApplyPerSkillCast(1000, 500, SkillIds.WZ_METEOR, b));
    }

    [Fact]
    public void Per_skill_cast_floors_at_zero()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bSkillVariableCast,WZ_METEOR,-9999;", b);
        Assert.Equal((0, 0), SkillCastTimingService.ApplyPerSkillCast(1000, 0, SkillIds.WZ_METEOR, b));
    }

    [Fact]
    public void No_bundle_is_passthrough()
        => Assert.Equal((1000, 500), SkillCastTimingService.ApplyPerSkillCast(1000, 500, SkillIds.WZ_STORMGUST, null));

    // ---- SA_ABRACADABRA zero cast + delay ----

    [Fact]
    public void Abracadabra_has_zero_cast_and_delay()
    {
        var svc = new SkillCastTimingService(new SkillDb(),
            new BattleConfigService(NullLogger<BattleConfigService>.Instance), sc: null);
        var caster = new PlayerEntity(1, 1, "Sage", Guid.NewGuid(), 0, 0, 0);
        caster.Stats.Dex = 50; caster.Stats.IntStat = 50;

        Assert.Equal(0, svc.VfCastFix(caster, 5000, SkillIds.SA_ABRACADABRA, 1));
        Assert.Equal(0, svc.DelayFix(caster, SkillIds.SA_ABRACADABRA, 1));
    }

    // ---- extractor parse ----

    [Fact]
    public void Extractor_parses_per_skill_flat_cast()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bSkillVariableCast,WZ_METEOR,-300; bonus2 bSkillFixedCast,WZ_METEOR,-150;", b);
        Assert.Equal(-300, b.SkillVarCast.GetValueOrDefault(SkillIds.WZ_METEOR));
        Assert.Equal(-150, b.SkillFixCast.GetValueOrDefault(SkillIds.WZ_METEOR));
    }
}
