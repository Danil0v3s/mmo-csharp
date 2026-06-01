using System;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-09 — renewal PC ASPD (<c>status_base_amotion_pc</c>, status.cpp:2310)
/// + the BL_PC amotion conversion (status.cpp:6147-6175) and dmotion
/// (status.cpp:6593). Replaces the prior <c>*540/590</c> heuristic.
/// </summary>
public class Combat09AspdTests
{
    // Reference computation of the renewal amotion (no SC / skill / aspd_add).
    private static int Expected(int aspdBase, int dex, int agi, bool ranged, int aspdRate2 = 0, int aspdAdd = 0)
    {
        double div = ranged ? 7.0 : 5.0;
        double temp = Math.Sqrt(dex * (double)dex / div + agi * (double)agi * 0.5) * 0.25 + 196.0;
        int aspd = (int)temp - Math.Min(aspdBase, 200);
        aspd += Math.Max(195 - aspd, 2) * aspdRate2 / 100;
        int amotion = 2000 - aspd * 10 - 10 * aspdAdd;
        return Math.Clamp(amotion, 95, 4000);
    }

    [Theory]
    // Novice/Fist base 40, melee divisor 5.
    [InlineData(40, 1, 1, false)]
    [InlineData(40, 50, 50, false)]
    [InlineData(40, 99, 99, false)]
    // Knight/1hSword base 47.
    [InlineData(47, 80, 60, false)]
    // Bow → ranged divisor 7.
    [InlineData(57, 90, 90, true)]
    public void RenewalPcAmotion_matchesHandComputed(int aspdBase, int dex, int agi, bool ranged)
    {
        int weaponType = ranged ? 11 /*Bow*/ : 0 /*Fist*/;
        int got = StatusCalcService.RenewalPcAmotion(aspdBase, dex, agi, weaponType, aspdRate2: 0, aspdAddVal: 0);
        Assert.Equal(Expected(aspdBase, dex, agi, ranged), got);
    }

    [Fact]
    public void HigherAgi_LowersAmotion_FollowingSqrtCurve()
    {
        // Same job/weapon/dex; AGI 99 must be strictly faster than AGI 1.
        int low = StatusCalcService.RenewalPcAmotion(40, 30, 1, 0, 0, 0);
        int high = StatusCalcService.RenewalPcAmotion(40, 30, 99, 0, 0, 0);
        Assert.True(high < low, $"high-AGI amotion {high} should be < low-AGI {low}");
    }

    [Fact]
    public void HigherDex_LowersAmotion()
    {
        int low = StatusCalcService.RenewalPcAmotion(40, 1, 30, 0, 0, 0);
        int high = StatusCalcService.RenewalPcAmotion(40, 99, 30, 0, 0, 0);
        Assert.True(high < low);
    }

    [Fact]
    public void RangedWeapon_UsesDex7Divisor_SlowerThanMeleeAtSameStats()
    {
        // dex² /7 (ranged) < dex² /5 (melee) → smaller temp → fewer aspd points
        // → higher amotion (slower) for the same DEX-heavy build.
        int melee = StatusCalcService.RenewalPcAmotion(50, 120, 1, 0, 0, 0);   // Fist
        int ranged = StatusCalcService.RenewalPcAmotion(50, 120, 1, 11, 0, 0); // Bow
        Assert.True(ranged > melee, $"ranged {ranged} should be slower (higher) than melee {melee}");
    }

    [Fact]
    public void AspdRate2_And_AspdAdd_SpeedUp()
    {
        int baseV = StatusCalcService.RenewalPcAmotion(40, 50, 50, 0, 0, 0);
        int withRate = StatusCalcService.RenewalPcAmotion(40, 50, 50, 0, aspdRate2: 10, aspdAddVal: 0);
        int withAdd = StatusCalcService.RenewalPcAmotion(40, 50, 50, 0, aspdRate2: 0, aspdAddVal: 5);
        Assert.True(withRate < baseV);
        Assert.Equal(baseV - 50, withAdd); // aspd_add: amotion -= 10*5
    }

    [Fact]
    public void Amotion_ClampedToFloor95()
    {
        // Absurd stats drive the aspd value high → amotion floored at 95.
        int got = StatusCalcService.RenewalPcAmotion(40, 9999, 9999, 0, 999, 0);
        Assert.Equal(95, got);
    }

    [Theory]
    [InlineData(1, 796)]    // 800 - 4 = 796
    [InlineData(100, 400)]  // 800 - 400 = 400 (floor)
    [InlineData(200, 400)]  // 800 - 800 = 0 → clamp 400
    [InlineData(0, 800)]    // ceiling
    public void RenewalPcDmotion_capsTo400_800(int agi, int expected)
        => Assert.Equal(expected, StatusCalcService.RenewalPcDmotion(agi));

    [Fact]
    public void CalcPc_setsAdelay_to2xAmotion_andRenewalDmotion()
    {
        var calc = new StatusCalcService(); // no cache → Fist base 40
        var pc = new PlayerEntity(1, 1, "H", Guid.NewGuid(), 0, 0, 0);
        var inputs = new PcBaseInputs(
            BaseLevel: 1, JobLevel: 1,
            Str: 1, Agi: 30, Vit: 1, Int: 1, Dex: 20, Luk: 1,
            Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
            WeaponAtkMin: 0, WeaponAtkMax: 0, EquipDef: 0, EquipMdef: 0, AttackRange: 1);
        calc.CalcPc(pc, inputs);

        int expectedAmotion = StatusCalcService.RenewalPcAmotion(40, 20, 30, 0, 0, 0);
        Assert.Equal(expectedAmotion, pc.Stats.Amotion);
        Assert.Equal(expectedAmotion * 2, pc.Stats.Adelay);     // AMOTION_DIVIDER_PC
        Assert.Equal(800 - 4 * 30, pc.Stats.Dmotion);            // 680
    }
}
