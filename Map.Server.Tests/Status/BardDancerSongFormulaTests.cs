using System;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// SC-03 — Bard/Dancer song magnitudes (rAthena status.cpp:10721+) after
/// collapsing the duplicate (Wave4a/Wave32) registrations to a single body.
/// </summary>
public class BardDancerSongFormulaTests
{
    private static readonly StatusEffectRegistry Reg = new();

    private static PlayerEntity Pc()
    {
        var pc = new PlayerEntity(1, 1, "P", Guid.NewGuid(), 0, 0, 0);
        return pc;
    }

    private static StatusChange Apply(StatusType type, int val1, Entity target, Entity? source = null)
    {
        var sc = new StatusChange { Type = type, Val1 = val1 };
        Reg.Get(type)!.OnStart(target, sc, source);
        return sc;
    }

    // ---- Assncros: val2 = val1<10 ? val1*2-1 : 20 ----

    [Theory]
    [InlineData(5, 9)]    // 5*2-1
    [InlineData(9, 17)]   // 9*2-1
    [InlineData(10, 20)]  // cap
    public void Assncros_Val2_AndAspdRate(int val1, int expectedVal2)
    {
        var pc = Pc();
        var sc = Apply(StatusType.Assncros, val1, pc);
        Assert.Equal(expectedVal2, sc.Val2);
        Assert.Equal(expectedVal2, pc.Stats.AspdRate);
    }

    // ---- Whistle: val2 = 18+2*val1 Flee, val3 = (val1+1)/2 Flee2 (NOT x10) ----

    [Fact]
    public void Whistle_Flee_And_Flee2_NotTenfold()
    {
        var pc = Pc();
        var sc = Apply(StatusType.Whistle, 5, pc);
        Assert.Equal(28, sc.Val2);              // 18 + 2*5
        Assert.Equal(3, sc.Val3);               // (5+1)/2 — not 30
        Assert.Equal(28, pc.Stats.Flee);
        Assert.Equal(3, pc.Stats.Flee2);
    }

    // ---- Appleidun: renewal val2 = (5+2*val1) + casterVit/10 + lesson/2 ----

    [Fact]
    public void Appleidun_RenewalHpRate_WithCasterVitAndLesson()
    {
        var caster = Pc();
        caster.Stats.Vit = 80;
        caster.LearnedSkills[SkillIds.BA_MUSICALLESSON] = 10;

        var target = Pc();
        target.Stats.MaxHp = 1000;

        var sc = Apply(StatusType.Appleidun, 5, target, caster);
        // (5 + 2*5) + (80/10) + (10/2) = 15 + 8 + 5 = 28 (HP rate %).
        Assert.Equal(28, sc.Val2);
        Assert.Equal(1000 + 1000 * 28 / 100, target.Stats.MaxHp); // 1280
    }

    [Fact]
    public void Appleidun_NoCaster_UsesBaseFormulaOnly()
    {
        var target = Pc();
        target.Stats.MaxHp = 1000;
        var sc = Apply(StatusType.Appleidun, 5, target, source: null);
        // No caster → no vit/lesson terms: 5 + 2*5 = 15.
        Assert.Equal(15, sc.Val2);
        Assert.Equal(1000 + 150, target.Stats.MaxHp);
    }

    [Fact]
    public void Appleidun_RespectsPrefilledVal2()
    {
        var target = Pc();
        target.Stats.MaxHp = 1000;
        var sc = new StatusChange { Type = StatusType.Appleidun, Val1 = 5, Val2 = 40 };
        Reg.Get(StatusType.Appleidun)!.OnStart(target, sc, null);
        Assert.Equal(40, sc.Val2); // caller-supplied magnitude respected
        Assert.Equal(1000 + 400, target.Stats.MaxHp);
    }

    // ---- Drumbattle (single surviving copy): val2 Atk, val3 Def ----

    [Fact]
    public void Drumbattle_AppliesAtkAndDef()
    {
        var pc = Pc();
        pc.Stats.WatkMin = pc.Stats.WatkMax = 100;
        var sc = Apply(StatusType.Drumbattle, 5, pc);
        Assert.Equal(40, sc.Val2);   // 15 + 5*5
        Assert.Equal(75, sc.Val3);   // 5*15
        Assert.Equal(140, pc.Stats.WatkMin);
        Assert.Equal(140, pc.Stats.WatkMax);
        Assert.Equal(75, pc.Stats.Def);
    }

    // ---- Verify the other winning copies still match rAthena ----

    [Fact]
    public void Humming_Hit_4xVal1()
    {
        var pc = Pc();
        var sc = Apply(StatusType.Humming, 5, pc);
        Assert.Equal(20, sc.Val2);
        Assert.Equal(20, pc.Stats.Hit);
    }

    [Fact]
    public void Fortune_Cri_Val1x10()
    {
        var pc = Pc();
        var sc = Apply(StatusType.Fortune, 5, pc);
        Assert.Equal(50, sc.Val2);
        Assert.Equal(50, pc.Stats.Cri);
    }

    [Fact]
    public void Service4u_MaxSpPercent()
    {
        var pc = Pc();
        pc.Stats.MaxSp = 1000;
        var sc = Apply(StatusType.Service4u, 5, pc);
        Assert.Equal(14, sc.Val2);   // val1<10 ? 9+val1 : 20 → 14 (%)
        Assert.Equal(1000 + 140, pc.Stats.MaxSp);
    }

    [Fact]
    public void Dontforgetme_AspdPenalty()
    {
        var pc = Pc();
        pc.Stats.AspdRate = 200;
        var sc = Apply(StatusType.Dontforgetme, 5, pc);
        Assert.Equal(151, sc.Val2);          // 1 + 30*5
        Assert.Equal(200 - 151 / 10, pc.Stats.AspdRate); // -15
    }
}
