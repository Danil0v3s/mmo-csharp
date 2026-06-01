using System;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// SC-05 — Sorcerer elemental-spirit *_OPTION buffs set a FIXED Val2
/// (equip-Atk / MATK / HP-rate% / bolt-id), NOT the generator's +Val1 stat.
/// (Element-change, bolt-autocast, and Wind* %-effects are SC-16.)
/// </summary>
public class SorcererOptionFormulaTests
{
    private static readonly StatusEffectRegistry Reg = new();

    private static StatusChange Apply(StatusType type, Entity target, int val1 = 5)
    {
        var sc = new StatusChange { Type = type, Val1 = val1 };
        Reg.Get(type)!.OnStart(target, sc, null);
        return sc;
    }

    // ---- equip-Atk options: fixed Val2 → WatkMin/Max (NOT +Val1 Batk) ----

    [Theory]
    [InlineData(StatusType.PyrotechnicOption, 60)]
    [InlineData(StatusType.HeaterOption, 120)]
    [InlineData(StatusType.TropicOption, 180)]
    public void EquipAtkOption_SetsFixedVal2_AppliesToWatk_NotBatk(StatusType type, int expectedVal2)
    {
        var pc = new PlayerEntity(1, 1, "P", Guid.NewGuid(), 0, 0, 0);
        var sc = Apply(type, pc);
        Assert.Equal(expectedVal2, sc.Val2);                 // fixed, Val1-independent
        Assert.Equal(expectedVal2, pc.Stats.WatkMin);
        Assert.Equal(expectedVal2, pc.Stats.WatkMax);
        Assert.Equal(0, pc.Stats.Batk);                      // no phantom +Val1 Batk

        Reg.Get(type)!.OnEnd(pc, sc);
        Assert.Equal(0, pc.Stats.WatkMin);                   // reverts cleanly
    }

    // ---- MATK options: fixed Val2 → MatkMin/Max ----

    [Theory]
    [InlineData(StatusType.AquaplayOption, 40)]
    [InlineData(StatusType.CoolerOption, 80)]
    [InlineData(StatusType.ChillyAirOption, 120)]
    [InlineData(StatusType.BlastOption, 20)]
    public void MatkOption_SetsFixedVal2_AppliesToMatk(StatusType type, int expectedVal2)
    {
        var pc = new PlayerEntity(1, 1, "P", Guid.NewGuid(), 0, 0, 0);
        var sc = Apply(type, pc);
        Assert.Equal(expectedVal2, sc.Val2);
        Assert.Equal(expectedVal2, pc.Stats.MatkMin);
        Assert.Equal(expectedVal2, pc.Stats.MatkMax);
        Assert.Equal(0, pc.Stats.Batk);                      // no phantom +Val1
        Assert.Equal(0, pc.Stats.AspdRate);                  // (Blast was +Val1 AspdRate before)

        Reg.Get(type)!.OnEnd(pc, sc);
        Assert.Equal(0, pc.Stats.MatkMin);
    }

    // ---- HP-rate options: fixed Val2 % → MaxHp (NOT +Val1 flat) ----

    [Theory]
    [InlineData(StatusType.PetrologyOption, 5)]
    [InlineData(StatusType.CursedSoilOption, 10)]
    public void HpRateOption_SetsFixedPercent_AppliesToMaxHp(StatusType type, int pct)
    {
        var pc = new PlayerEntity(1, 1, "P", Guid.NewGuid(), 0, 0, 0);
        pc.Stats.MaxHp = 1000;
        var sc = Apply(type, pc);
        Assert.Equal(pct, sc.Val2);
        Assert.Equal(1000 + 1000 * pct / 100, pc.Stats.MaxHp); // 5%→1050, 10%→1100

        Reg.Get(type)!.OnEnd(pc, sc);
        Assert.Equal(1000, pc.Stats.MaxHp);
    }

    // ---- presence-only options: correct Val2, no phantom stat ----

    [Fact]
    public void WildStormOption_StoresBoltSkillId_NoStat()
    {
        var pc = new PlayerEntity(1, 1, "P", Guid.NewGuid(), 0, 0, 0);
        var sc = Apply(StatusType.WildStormOption, pc);
        Assert.Equal(SkillIds.MG_LIGHTNINGBOLT, sc.Val2);
        Assert.Equal(0, pc.Stats.AspdRate);   // no phantom +Val1
    }

    [Fact]
    public void WindStepOption_StoresVal2_50_NoStat()
    {
        var pc = new PlayerEntity(1, 1, "P", Guid.NewGuid(), 0, 0, 0);
        var sc = Apply(StatusType.WindStepOption, pc);
        Assert.Equal(50, sc.Val2);
        Assert.Equal(0, pc.Stats.AspdRate);
        Assert.Equal(0, pc.Stats.Flee);
    }

    [Fact]
    public void WindCurtainOption_StoresVal2_100_NoStat()
    {
        var pc = new PlayerEntity(1, 1, "P", Guid.NewGuid(), 0, 0, 0);
        var sc = Apply(StatusType.WindCurtainOption, pc);
        Assert.Equal(100, sc.Val2);
        Assert.Equal(0, pc.Stats.Str);   // no phantom six-stat +Val1
    }
}
