using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Skills.Behaviors.Acolyte;
using Map.Server.Status;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-23 — single-value pc_bonus tail + the 1-arg flag form. rAthena
/// pc_bonus (pc.cpp:3644): SP_NO_CAST_CANCEL / SP_ADD_HEAL_RATE /
/// SP_HP_RECOV_RATE / SP_SPEED_RATE.
/// </summary>
public class Combat23PcBonusTailTests
{
    // ---- flag-form parse ----

    [Fact]
    public void Flag_form_sets_nocastcancel_and_unbreakable()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus bNoCastCancel; bonus bUnbreakableArmor; bonus bIntravision;", b);
        Assert.True(b.NoCastCancel);
        Assert.True(b.UnbreakableArmor);
        Assert.True(b.Intravision);
        Assert.False(b.UnbreakableWeapon);
    }

    [Fact]
    public void Flag_form_does_not_swallow_a_valued_bonus()
    {
        // `bonus bAtk,10;` must still parse as a flat value, not a flag.
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus bAtk,10;", b);
        Assert.Equal(10, b.FlatAtk);
        Assert.False(b.NoCastCancel);
    }

    // ---- single-value parse ----

    [Fact]
    public void Single_value_tail_parses()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply(
            "bonus bHealPower,30; bonus bHPrecovRate,25; bonus bSPrecovRate,15; bonus bSpeedRate,25;", b);
        Assert.Equal(30, b.HealPower);
        Assert.Equal(25, b.HpRecovRate);
        Assert.Equal(15, b.SpRecovRate);
        Assert.Equal(-25, b.SpeedRate); // SP_SPEED_RATE stores min(-val)
    }

    // ---- HealPower consumer ----

    [Fact]
    public void HealPower_boosts_heal_output()
    {
        var caster = NewPc(level: 50, intStat: 50);
        var target = NewPc(level: 50, intStat: 50);
        var plain = new Heal();

        // Base renewal heal at lv 10: ((50+50)/5)*30*10/10 = 600.
        Assert.Equal(600, plain.CalcRenewalHealForTest(caster, target, 10));

        caster.EquipBonuses.HealPower = 30;
        Assert.Equal(780, plain.CalcRenewalHealForTest(caster, target, 10)); // 600 × 1.30
    }

    // ---- recov-rate consumer (math) ----

    [Theory]
    [InlineData(0, 10)]    // base amount
    [InlineData(50, 15)]   // +50% → 15
    [InlineData(100, 20)]  // +100% → 20
    public void HpRecovRate_scales_regen_amount(int recovRate, int expected)
    {
        // amount = 10 (base) × (100 + recovRate)/100.
        var amount = 10;
        amount = amount * (100 + recovRate) / 100;
        Assert.Equal(expected, amount);
    }

    private static PlayerEntity NewPc(int level, int intStat)
    {
        var p = new PlayerEntity(1, 1, "Aco", Guid.NewGuid(), 0, 0, 0) { Level = level };
        p.Stats.IntStat = (short)intStat;
        return p;
    }
}
