using System;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// SC-06 — Star Emperor stance / Royal Guard Inspiration / Banding / Nen
/// magnitudes (rAthena status.cpp init arms + consumers) replacing the
/// generator's phantom +Val1.
/// </summary>
public class SC06StanceFormulaTests
{
    private static readonly StatusEffectRegistry Reg = new();

    private static PlayerEntity Pc() => new(1, 1, "P", Guid.NewGuid(), 0, 0, 0);

    private static StatusChange Apply(StatusType type, Entity target, int val1 = 5)
    {
        var sc = new StatusChange { Type = type, Val1 = val1 };
        Reg.Get(type)!.OnStart(target, sc, null);
        return sc;
    }

    // ---- Sunstance: Val2 = 2+Val1 = ATK% on Batk + Watk ----

    [Fact]
    public void Sunstance_AtkPercent_OnBatkAndWatk()
    {
        var pc = Pc();
        pc.Stats.Batk = 100; pc.Stats.WatkMin = 100; pc.Stats.WatkMax = 100;
        var sc = Apply(StatusType.Sunstance, pc);
        Assert.Equal(7, sc.Val2);                       // 2 + 5
        Assert.Equal(107, pc.Stats.Batk);               // +7%
        Assert.Equal(107, pc.Stats.WatkMin);
        Assert.Equal(107, pc.Stats.WatkMax);

        Reg.Get(StatusType.Sunstance)!.OnEnd(pc, sc);
        Assert.Equal(100, pc.Stats.Batk);               // reverts cleanly
        Assert.Equal(100, pc.Stats.WatkMin);
    }

    // ---- Starstance: Val2 = 4+2*Val1 ASPD ----

    [Fact]
    public void Starstance_AspdRate_Val2_NotVal1()
    {
        var pc = Pc();
        var sc = Apply(StatusType.Starstance, pc);
        Assert.Equal(14, sc.Val2);                      // 4 + 2*5
        Assert.Equal(14, pc.Stats.AspdRate);
        Reg.Get(StatusType.Starstance)!.OnEnd(pc, sc);
        Assert.Equal(0, pc.Stats.AspdRate);
    }

    // ---- Lunarstance / Universestance (already-correct Wave61 bodies) ----

    [Fact]
    public void Lunarstance_MaxHpPercent()
    {
        var pc = Pc(); pc.Stats.MaxHp = 1000;
        var sc = Apply(StatusType.Lunarstance, pc);
        Assert.Equal(7, sc.Val2);
        Assert.Equal(1070, pc.Stats.MaxHp);
        Reg.Get(StatusType.Lunarstance)!.OnEnd(pc, sc);
        Assert.Equal(1000, pc.Stats.MaxHp);
    }

    [Fact]
    public void Universestance_FlatAllStat()
    {
        var pc = Pc();
        pc.Stats.Str = 10; pc.Stats.Agi = 10; pc.Stats.Vit = 10;
        pc.Stats.IntStat = 10; pc.Stats.Dex = 10; pc.Stats.Luk = 10;
        var sc = Apply(StatusType.Universestance, pc);
        Assert.Equal(7, sc.Val2);
        Assert.Equal(17, pc.Stats.Str);
        Assert.Equal(17, pc.Stats.Luk);
    }

    // ---- Inspiration: Val2 = 40*Val1 (ATK/MATK), Val3 = 6*Val1 (all-stat) ----

    [Fact]
    public void Inspiration_AtkMatk_AndAllStat()
    {
        var pc = Pc();
        pc.Stats.Batk = 100; pc.Stats.MatkMin = 50; pc.Stats.MatkMax = 50;
        pc.Stats.Str = 10; pc.Stats.MaxHp = 1000;
        var sc = Apply(StatusType.Inspiration, pc);
        Assert.Equal(200, sc.Val2);                     // 40*5
        Assert.Equal(30, sc.Val3);                      // 6*5
        Assert.Equal(300, pc.Stats.Batk);              // +Val2
        Assert.Equal(250, pc.Stats.MatkMin);
        Assert.Equal(40, pc.Stats.Str);                // +Val3
        Assert.Equal(1020, pc.Stats.MaxHp);            // +4*Val1

        Reg.Get(StatusType.Inspiration)!.OnEnd(pc, sc);
        Assert.Equal(100, pc.Stats.Batk);
        Assert.Equal(10, pc.Stats.Str);
        Assert.Equal(1000, pc.Stats.MaxHp);
    }

    // ---- Banding: best-effort count, no faked +Val1 Def ----

    [Fact]
    public void Banding_StoresBestEffortCount_NoDefFake()
    {
        var pc = Pc(); pc.Stats.Def = 50;
        var sc = Apply(StatusType.Banding, pc);
        Assert.Equal(1, sc.Val2);          // best-effort banded count
        Assert.Equal(50, pc.Stats.Def);    // no phantom +Val1 Def
    }

    // ---- Nen: verified +Val1 STR/INT (status.cpp:6540/6749) ----

    [Fact]
    public void Nen_AddsVal1ToStrInt()
    {
        var pc = Pc(); pc.Stats.Str = 10; pc.Stats.IntStat = 10;
        var sc = Apply(StatusType.Nen, pc);
        Assert.Equal(15, pc.Stats.Str);
        Assert.Equal(15, pc.Stats.IntStat);
        Reg.Get(StatusType.Nen)!.OnEnd(pc, sc);
        Assert.Equal(10, pc.Stats.Str);
    }
}
