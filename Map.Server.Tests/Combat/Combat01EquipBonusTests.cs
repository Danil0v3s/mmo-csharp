using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-01 — equip / card flat-derived bonuses must reach
/// <see cref="StatusCalcService.CalcPc"/> and the bundle must capture the
/// param-stat keys. Baseline anchors come from
/// <see cref="StatusCalcServiceTests"/> (Novice Lv1, all stats 1):
/// Hit 177, Flee 102, Cri 13 (×10), Batk 1, MaxHp 40, Amotion 590.
/// </summary>
public class Combat01EquipBonusTests
{
    private static PcBaseInputs NoviceLv1() => new(
        BaseLevel: 1, JobLevel: 1,
        Str: 1, Agi: 1, Vit: 1, Int: 1, Dex: 1, Luk: 1,
        Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
        WeaponAtkMin: 17, WeaponAtkMax: 17,
        EquipDef: 10, EquipMdef: 0, AttackRange: 1);

    private static PlayerEntity NewPc()
        => new(1, 1, "Hero", System.Guid.NewGuid(), mapId: 0, x: 0, y: 0);

    // ---- BonusScriptExtractor: param + flat capture ----

    [Theory]
    [InlineData("bonus bStr,10;", "str", 10)]
    [InlineData("bonus bInt,7;", "int", 7)]
    [InlineData("bonus bLuk,-3;", "luk", -3)]
    [InlineData("bonus bPow,4;", "pow", 4)]
    public void Extractor_capturesParamBonuses(string script, string which, int expected)
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply(script, b);
        var got = which switch
        {
            "str" => b.Str, "int" => b.IntStat, "luk" => b.Luk, "pow" => b.Pow,
            _ => int.MinValue,
        };
        Assert.Equal(expected, got);
    }

    [Fact]
    public void Extractor_capturesFlatDerivedBonuses()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply(
            "bonus bHit,30; bonus bFlee,15; bonus bCritical,20; bonus bAtk,50; "
            + "bonus bMatk,8; bonus bMaxHP,500; bonus bMaxHPrate,10; bonus bAspd,1; "
            + "bonus bAspdRate,5;", b);
        Assert.Equal(30, b.FlatHit);
        Assert.Equal(15, b.FlatFlee);
        Assert.Equal(20, b.FlatCritical);
        Assert.Equal(50, b.FlatAtk);
        Assert.Equal(8, b.FlatMatk);
        Assert.Equal(500, b.FlatMaxHp);
        Assert.Equal(10, b.MaxHpRate);
        Assert.Equal(1, b.FlatAspd);
        Assert.Equal(5, b.FlatAspdRate);
    }

    [Fact]
    public void Reset_clearsParamBonuses()
    {
        var b = new EquipBonusBundle { Str = 5, Dex = 9, Crt = 2 };
        b.Reset();
        Assert.Equal(0, b.Str);
        Assert.Equal(0, b.Dex);
        Assert.Equal(0, b.Crt);
    }

    // ---- CalcPc: flat-derived folds (idempotent, applied every recalc) ----

    [Fact]
    public void CalcPc_appliesFlatHitFleeCriBatkMatk()
    {
        var calc = new StatusCalcService();
        var pc = NewPc();
        pc.EquipBonuses.FlatHit = 30;
        pc.EquipBonuses.FlatFlee = 15;
        pc.EquipBonuses.FlatCritical = 20;   // display points → +200 internal
        pc.EquipBonuses.FlatAtk = 50;
        pc.EquipBonuses.FlatMatk = 8;

        calc.CalcPc(pc, NoviceLv1());
        var s = pc.Stats;

        Assert.Equal(177 + 30, s.Hit);
        Assert.Equal(102 + 15, s.Flee);
        Assert.Equal(13 + 200, s.Cri);
        Assert.Equal(1 + 50, s.Batk);
        // Novice Lv1 MatkMin = int + int/2 + dex/5 + luk/3 + level/4 + 5*spl
        //                    = 1 + 0 + 0 + 0 + 0 + 0 = 1, then + bMatk 8.
        Assert.Equal(1 + 8, s.MatkMin);
        Assert.Equal(1 + 8, s.MatkMax);
    }

    [Fact]
    public void CalcPc_appliesMaxHpRateThenFlat()
    {
        var calc = new StatusCalcService();
        var pc = NewPc();
        pc.EquipBonuses.MaxHpRate = 10;   // +10 %
        pc.EquipBonuses.FlatMaxHp = 500;  // +500 flat, applied AFTER rate

        calc.CalcPc(pc, NoviceLv1());
        // base 40 → *110/100 = 44 → +500 = 544 (rate before flat).
        Assert.Equal(40 * 110 / 100 + 500, pc.Stats.MaxHp);
        Assert.Equal(544, pc.Stats.MaxHp);
    }

    [Fact]
    public void CalcPc_appliesAspdRateAndFlat_lowersAmotion()
    {
        var calc = new StatusCalcService();
        var pc = NewPc();
        pc.EquipBonuses.FlatAspdRate = 10; // -10 %
        pc.EquipBonuses.FlatAspd = 2;      // -20 ms (×10)

        calc.CalcPc(pc, NoviceLv1());
        // base 590 → *90/100 = 531 → -20 = 511. Faster than the 590 base.
        Assert.Equal(590 * 90 / 100 - 20, pc.Stats.Amotion);
        Assert.True(pc.Stats.Amotion < 590);
    }

    [Fact]
    public void CalcPc_isIdempotent_acrossRepeatedRecalcs()
    {
        // The read-back recalc callers (level-up / SC / job-change) feed
        // CalcPc its own previous output. The flat-derived folds must NOT
        // accumulate — two calls produce the same numbers.
        var calc = new StatusCalcService();
        var pc = NewPc();
        pc.EquipBonuses.FlatHit = 30;
        pc.EquipBonuses.FlatAtk = 50;
        pc.EquipBonuses.MaxHpRate = 10;
        pc.EquipBonuses.FlatMaxHp = 500;

        calc.CalcPc(pc, NoviceLv1());
        var hit1 = pc.Stats.Hit; var batk1 = pc.Stats.Batk; var hp1 = pc.Stats.MaxHp;
        calc.CalcPc(pc, NoviceLv1());

        Assert.Equal(hit1, pc.Stats.Hit);
        Assert.Equal(batk1, pc.Stats.Batk);
        Assert.Equal(hp1, pc.Stats.MaxHp);
        Assert.Equal(177 + 30, pc.Stats.Hit); // not 177+60
    }

    [Fact]
    public void CalcPc_doesNotYetApplyParamStats_boundaryForCombat10()
    {
        // Param stats (bStr..bLuk) need the base→final split (COMBAT-10) to
        // apply without double-counting / wiping SC stat mods. CalcPc must
        // leave the primary stat at the base value for now.
        var calc = new StatusCalcService();
        var pc = NewPc();
        pc.EquipBonuses.Str = 10;

        calc.CalcPc(pc, NoviceLv1());
        Assert.Equal(1, pc.Stats.Str); // base only; +10 lands with COMBAT-10
    }

    [Fact]
    public void CalcPc_emptyBundle_matchesBaseline()
    {
        // Regression: a PC with no bonus-script gear is unchanged.
        var calc = new StatusCalcService();
        var pc = NewPc();
        calc.CalcPc(pc, NoviceLv1());
        Assert.Equal(177, pc.Stats.Hit);
        Assert.Equal(13, pc.Stats.Cri);
        Assert.Equal(1, pc.Stats.Batk);
        Assert.Equal(40, pc.Stats.MaxHp);
        Assert.Equal(590, pc.Stats.Amotion);
    }
}
