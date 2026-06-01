using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Skills;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-07 — renewal variable-cast DEX/INT sqrt reduction + equip/card cast
/// bonuses. rAthena skill_vfcastfix (skill.cpp:20444) / skill_delayfix.
/// </summary>
public class Combat07CastTimingTests
{
    private const int Scale = 530; // battle_config.vcast_stat_scale

    private static int ExpectedSqrt(int time, int dex, int intStat)
        => (int)(time * (1 - Math.Sqrt((double)(dex * 2 + intStat) / Scale)));

    // ---- ApplyVariableCast: DEX/INT sqrt ----

    [Fact]
    public void VariableCast_dex_int_sqrt_reduction()
    {
        // DEX 130 / INT 99 / 5000ms → ~885ms (ticket worked example).
        var r = SkillCastTimingService.ApplyVariableCast(5000, 130, 99, Scale, dexBypass: false, b: null);
        Assert.Equal(ExpectedSqrt(5000, 130, 99), r);
        Assert.InRange(r, 880, 890);
    }

    [Fact]
    public void VariableCast_dex_bypass_skips_sqrt()
    {
        var r = SkillCastTimingService.ApplyVariableCast(5000, 130, 99, Scale, dexBypass: true, b: null);
        Assert.Equal(5000, r);
    }

    [Fact]
    public void VariableCast_caps_reduction_at_100pct()
    {
        // Huge stats → sqrt arg > 1 → clamp to instant (0), not negative.
        var r = SkillCastTimingService.ApplyVariableCast(5000, 9999, 9999, Scale, dexBypass: false, b: null);
        Assert.Equal(0, r);
    }

    [Fact]
    public void VariableCast_card_rate_and_flat_ms()
    {
        // dex/int 0 → no sqrt. VarCastRate -30 (faster), AddVarCastMs 200 (faster).
        var b = new EquipBonusBundle { VarCastRate = -30, AddVarCastMs = 200 };
        // 1000 - 200 = 800, then * (100-30)/100 = 560
        var r = SkillCastTimingService.ApplyVariableCast(1000, 0, 0, Scale, dexBypass: false, b: b);
        Assert.Equal((1000 - 200) * 70 / 100, r);
    }

    // ---- ApplyFixedCast: not DEX/INT reduced ----

    [Fact]
    public void FixedCast_rate_and_flat_ms()
    {
        var b = new EquipBonusBundle { FixCastRate = -40, AddFixCastMs = 100 };
        // 500 - 100 = 400, then * 60/100 = 240
        Assert.Equal((500 - 100) * 60 / 100, SkillCastTimingService.ApplyFixedCast(500, b));
        Assert.Equal(500, SkillCastTimingService.ApplyFixedCast(500, null));
    }

    // ---- ApplyDelayBonus ----

    [Fact]
    public void Delay_rate_bonus()
    {
        var b = new EquipBonusBundle { DelayRate = -20 };
        Assert.Equal(1000 * 80 / 100, SkillCastTimingService.ApplyDelayBonus(1000, b)); // 800
        Assert.Equal(1000, SkillCastTimingService.ApplyDelayBonus(1000, null));
    }

    // ---- integration through VfCastFix (real SkillDb; unknown skill → fixed 0) ----

    [Fact]
    public void VfCastFix_applies_sqrt_end_to_end()
    {
        var svc = new SkillCastTimingService(new SkillDb(),
            new BattleConfigService(NullLogger<BattleConfigService>.Instance), sc: null);
        var pc = new PlayerEntity(1, 1, "H", Guid.NewGuid(), 0, 0, 0);
        pc.Stats.Dex = 130; pc.Stats.IntStat = 99;
        // skillId 60000 is unknown → GetFixedCast 0 (no split), GetCastNoDex 0.
        Assert.Equal(ExpectedSqrt(5000, 130, 99), svc.VfCastFix(pc, 5000, 60000, 1));
    }

    [Fact]
    public void VfCastFix_applies_card_var_cast_rate()
    {
        var svc = new SkillCastTimingService(new SkillDb(),
            new BattleConfigService(NullLogger<BattleConfigService>.Instance), sc: null);
        var pc = new PlayerEntity(1, 1, "H", Guid.NewGuid(), 0, 0, 0);
        pc.Stats.Dex = 0; pc.Stats.IntStat = 0;           // no sqrt
        pc.EquipBonuses.VarCastRate = -30;                 // -30% var cast
        Assert.Equal(5000 * 70 / 100, svc.VfCastFix(pc, 5000, 60000, 1)); // 3500
    }
}
