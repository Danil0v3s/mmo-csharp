using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// Wave 97-1 — pins the rAthena formulas for the first batch of audited SCs:
/// Blessing, IncreaseAgi, DecreaseAgi, Provoke, Concentrate, Concentration,
/// Adrenaline, Twohandquicken, Angelus, Quagmire, Hawkeyes, Explosionspirits,
/// Assumptio, Kyrie, Truesight.
/// </summary>
public class Wave97Batch1FormulaTests
{
    [Fact]
    public void SC_BLESSING_normal_target_adds_Val1_to_str_int_dex()
    {
        // rAthena status.cpp:11566-11571: val2 = val1 for non-undead/demon.
        // Consumer adds val2 to str/int/dex.  Our handler derives the
        // delta from val1 when caller doesn't set the undead flag.
        var pc = MakePc(str: 20, agi: 0, vit: 0, intStat: 30, dex: 40, luk: 0);
        var h = new StatusEffectRegistry().Get(StatusType.Blessing)!;
        var sc = new StatusChange { Type = StatusType.Blessing, Val1 = 10 };
        h.OnStart(pc, sc, null);
        Assert.Equal(30, pc.Stats.Str);    // 20 + 10
        Assert.Equal(40, pc.Stats.IntStat); // 30 + 10
        Assert.Equal(50, pc.Stats.Dex);    // 40 + 10
        h.OnEnd(pc, sc);
        Assert.Equal(20, pc.Stats.Str);
        Assert.Equal(30, pc.Stats.IntStat);
        Assert.Equal(40, pc.Stats.Dex);
    }

    [Fact]
    public void SC_BLESSING_undead_branch_halves_str_int_dex()
    {
        // Caller signals undead/demon by passing Val4 = 1.
        var pc = MakePc(str: 50, agi: 0, vit: 0, intStat: 40, dex: 30, luk: 0);
        var h = new StatusEffectRegistry().Get(StatusType.Blessing)!;
        var sc = new StatusChange { Type = StatusType.Blessing, Val1 = 10, Val4 = 1 };
        h.OnStart(pc, sc, null);
        Assert.Equal(25, pc.Stats.Str);
        Assert.Equal(20, pc.Stats.IntStat);
        Assert.Equal(15, pc.Stats.Dex);
        h.OnEnd(pc, sc);
        Assert.Equal(50, pc.Stats.Str);
        Assert.Equal(40, pc.Stats.IntStat);
        Assert.Equal(30, pc.Stats.Dex);
    }

    [Fact]
    public void SC_INCREASEAGI_adds_2_plus_val1_to_Agi()
    {
        // rAthena status.cpp:10853: val2 = 2 + val1. Consumer (6843-6844) adds val2.
        var pc = MakePc(str: 0, agi: 50, vit: 0, intStat: 0, dex: 0, luk: 0);
        var h = new StatusEffectRegistry().Get(StatusType.IncreaseAgi)!;
        var sc = new StatusChange { Type = StatusType.IncreaseAgi, Val1 = 10 };
        h.OnStart(pc, sc, null);
        Assert.Equal(62, pc.Stats.Agi); // 50 + (2+10)
        Assert.Equal(12, sc.Val2);       // populated by OnStart
        h.OnEnd(pc, sc);
        Assert.Equal(50, pc.Stats.Agi);
    }

    [Fact]
    public void SC_DECREASEAGI_subtracts_2_plus_val1_from_Agi()
    {
        var pc = MakePc(str: 0, agi: 50, vit: 0, intStat: 0, dex: 0, luk: 0);
        var h = new StatusEffectRegistry().Get(StatusType.DecreaseAgi)!;
        var sc = new StatusChange { Type = StatusType.DecreaseAgi, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(43, pc.Stats.Agi); // 50 - (2+5)
        h.OnEnd(pc, sc);
        Assert.Equal(50, pc.Stats.Agi);
    }

    [Fact]
    public void SC_PROVOKE_player_caster_applies_proportional_batk_and_def_drop()
    {
        // rAthena status.cpp:11665-11668: val2 = 2+3*val1, val3 = 5+5*val1.
        // val2 % batk increase, val3 % def reduction.
        var pc = MakePcWithBatkDef(batk: 100, def: 100);
        var h = new StatusEffectRegistry().Get(StatusType.Provoke)!;
        var sc = new StatusChange { Type = StatusType.Provoke, Val1 = 10 };
        h.OnStart(pc, sc, null);
        // Val2 should be 2+3*10=32 %, Val3 = 5+5*10=55 %.
        Assert.Equal(32, sc.Val2);
        Assert.Equal(55, sc.Val3);
        // Batk: 100 + 100*32/100 = 132
        Assert.Equal(132, pc.Stats.Batk);
        // Def: 100 - 100*55/100 = 45
        Assert.Equal(45, pc.Stats.Def);
        h.OnEnd(pc, sc);
        Assert.Equal(100, pc.Stats.Batk);
        Assert.Equal(100, pc.Stats.Def);
    }

    [Fact]
    public void SC_CONCENTRATE_adds_proportional_agi_dex()
    {
        // rAthena status.cpp:11577: val2 = 2+val1. Consumer: agi += agi*val2/100, dex += dex*val2/100.
        var pc = MakePc(str: 0, agi: 100, vit: 0, intStat: 0, dex: 100, luk: 0);
        var h = new StatusEffectRegistry().Get(StatusType.Concentrate)!;
        var sc = new StatusChange { Type = StatusType.Concentrate, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(7, sc.Val2); // 2+5
        // Agi: 100 + (100-0)*7/100 = 107
        Assert.Equal(107, pc.Stats.Agi);
        Assert.Equal(107, pc.Stats.Dex);
        h.OnEnd(pc, sc);
        Assert.Equal(100, pc.Stats.Agi);
        Assert.Equal(100, pc.Stats.Dex);
    }

    [Fact]
    public void SC_CONCENTRATION_RE_applies_batk_hit_def_drop()
    {
        // RE rAthena: batk +(5+val1*2)% / hit +10*val1 flat / def -(5+val1*2)%.
        var pc = MakePcWithBatkDef(batk: 100, def: 100);
        var h = new StatusEffectRegistry().Get(StatusType.Concentration)!;
        var sc = new StatusChange { Type = StatusType.Concentration, Val1 = 5 };
        h.OnStart(pc, sc, null);
        // Pct = 5+5*2 = 15. Batk: 100 + 100*15/100 = 115.
        Assert.Equal(115, pc.Stats.Batk);
        // Hit += 50 (10*val1).
        Assert.Equal(50, pc.Stats.Hit);
        // Def: 100 - 100*15/100 = 85.
        Assert.Equal(85, pc.Stats.Def);
        h.OnEnd(pc, sc);
        Assert.Equal(100, pc.Stats.Batk);
        Assert.Equal(0, pc.Stats.Hit);
        Assert.Equal(100, pc.Stats.Def);
    }

    [Fact]
    public void SC_ADRENALINE_adds_hit_and_aspd_rate()
    {
        // rAthena: val3 = 200 (self-cast). Consumer hit += val1*3+5.
        var pc = MakePc(str: 0, agi: 0, vit: 0, intStat: 0, dex: 0, luk: 0);
        pc.Stats.AspdRate = 100;
        var h = new StatusEffectRegistry().Get(StatusType.Adrenaline)!;
        var sc = new StatusChange { Type = StatusType.Adrenaline, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(200, sc.Val3); // self-cast
        Assert.Equal(20, pc.Stats.Hit); // 5*3+5
        Assert.Equal(300, pc.Stats.AspdRate); // 100 + 200
        h.OnEnd(pc, sc);
        Assert.Equal(0, pc.Stats.Hit);
        Assert.Equal(100, pc.Stats.AspdRate);
    }

    [Fact]
    public void SC_TWOHANDQUICKEN_applies_scaled_aspd_plus_hit_plus_cri()
    {
        // rAthena val2 = 300; our port scales AspdRate by /10 → +30. Plus
        // +val1*2 Hit and +(2+val1)*10 Cri from status_calc_* consumers.
        var pc = MakePc(str: 0, agi: 0, vit: 0, intStat: 0, dex: 0, luk: 0);
        pc.Stats.AspdRate = 100;
        pc.Stats.Cri = 50;
        var h = new StatusEffectRegistry().Get(StatusType.Twohandquicken)!;
        var sc = new StatusChange { Type = StatusType.Twohandquicken, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(30, sc.Val2);            // project AspdRate convention (/10)
        Assert.Equal(130, pc.Stats.AspdRate); // 100 + 30
        Assert.Equal(10, pc.Stats.Hit);       // +val1*2
        Assert.Equal(50 + 70, pc.Stats.Cri);  // +(2+5)*10 = +70
        h.OnEnd(pc, sc);
        Assert.Equal(100, pc.Stats.AspdRate);
        Assert.Equal(0, pc.Stats.Hit);
        Assert.Equal(50, pc.Stats.Cri);
    }

    [Fact]
    public void SC_ANGELUS_renewal_adds_vit_scaled_Def2()
    {
        // rAthena renewal status_calc_def2:7878-7880: def2 += vit/2 * val2/100,
        // where val2 = 5*val1.  At vit=100, val1=10: delta = 50 * 50 / 100 = 25.
        var pc = MakePc(str: 0, agi: 0, vit: 100, intStat: 0, dex: 0, luk: 0);
        pc.Stats.Def2 = 100;
        var h = new StatusEffectRegistry().Get(StatusType.Angelus)!;
        var sc = new StatusChange { Type = StatusType.Angelus, Val1 = 10 };
        h.OnStart(pc, sc, null);
        Assert.Equal(50, sc.Val2);       // val2 = 5*val1
        Assert.Equal(125, pc.Stats.Def2); // 100 + 25
        h.OnEnd(pc, sc);
        Assert.Equal(100, pc.Stats.Def2);
    }

    [Fact]
    public void SC_QUAGMIRE_subtracts_5_times_val1_from_Agi_and_Dex()
    {
        // rAthena status.cpp:11643: val2 = 5*val1 for PC. Consumer subtracts val2.
        var pc = MakePc(str: 0, agi: 80, vit: 0, intStat: 0, dex: 80, luk: 0);
        var h = new StatusEffectRegistry().Get(StatusType.Quagmire)!;
        var sc = new StatusChange { Type = StatusType.Quagmire, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(25, sc.Val2);
        Assert.Equal(55, pc.Stats.Agi); // 80-25
        Assert.Equal(55, pc.Stats.Dex);
        h.OnEnd(pc, sc);
        Assert.Equal(80, pc.Stats.Agi);
        Assert.Equal(80, pc.Stats.Dex);
    }

    [Fact]
    public void SC_HAWKEYES_adds_val1_to_Dex_only()
    {
        var pc = MakePc(str: 0, agi: 0, vit: 0, intStat: 0, dex: 50, luk: 0);
        var h = new StatusEffectRegistry().Get(StatusType.Hawkeyes)!;
        var sc = new StatusChange { Type = StatusType.Hawkeyes, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(55, pc.Stats.Dex);
        Assert.Equal(0, pc.Stats.Hit); // no hit bonus per rAthena consumer
        h.OnEnd(pc, sc);
        Assert.Equal(50, pc.Stats.Dex);
    }

    [Fact]
    public void SC_EXPLOSIONSPIRITS_adds_75_plus_25_times_val1_to_Cri()
    {
        // rAthena val2 = 75 + 25*val1, applied directly to internal Cri
        // (no re-scaling — internal Cri is stored ×10 in both rAthena and
        // our port).  Val1=5 → +200 stored = +20 display crit.
        var pc = MakePc(str: 0, agi: 0, vit: 0, intStat: 0, dex: 0, luk: 0);
        pc.Stats.Cri = 30;
        var h = new StatusEffectRegistry().Get(StatusType.Explosionspirits)!;
        var sc = new StatusChange { Type = StatusType.Explosionspirits, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(200, sc.Val2);     // 75+25*5
        Assert.Equal(230, pc.Stats.Cri); // 30+200
        h.OnEnd(pc, sc);
        Assert.Equal(30, pc.Stats.Cri);
    }

    [Fact]
    public void SC_ASSUMPTIO_adds_val1_times_50_to_Def()
    {
        // rAthena renewal status_calc_def: def += val1*50.
        var pc = MakePcWithBatkDef(batk: 0, def: 100);
        var h = new StatusEffectRegistry().Get(StatusType.Assumptio)!;
        var sc = new StatusChange { Type = StatusType.Assumptio, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(250, sc.Val2);     // val1*50
        Assert.Equal(350, pc.Stats.Def); // 100 + 250
        h.OnEnd(pc, sc);
        Assert.Equal(100, pc.Stats.Def);
    }

    [Fact]
    public void SC_KYRIE_standard_uses_val1_2_plus_10_percent_max_hp()
    {
        var pc = MakePc(str: 0, agi: 0, vit: 0, intStat: 0, dex: 0, luk: 0);
        pc.Stats.MaxHp = 1000;
        var h = new StatusEffectRegistry().Get(StatusType.Kyrie)!;
        var sc = new StatusChange { Type = StatusType.Kyrie, Val1 = 10 };
        h.OnStart(pc, sc, null);
        // val2 = 1000 * (20+10) / 100 = 300, val3 = 10/2+5 = 10.
        Assert.Equal(300, sc.Val2);
        Assert.Equal(10, sc.Val3);
    }

    [Fact]
    public void SC_KYRIE_praefatio_uses_higher_hit_count()
    {
        var pc = MakePc(str: 0, agi: 0, vit: 0, intStat: 0, dex: 0, luk: 0);
        pc.Stats.MaxHp = 1000;
        var h = new StatusEffectRegistry().Get(StatusType.Kyrie)!;
        var sc = new StatusChange { Type = StatusType.Kyrie, Val1 = 5, Val4 = 10 };
        h.OnStart(pc, sc, null);
        // Praefatio: val2 = 1000*(10+10)/100 + 20 = 220; val3 = 6+5 = 11.
        Assert.Equal(220, sc.Val2);
        Assert.Equal(11, sc.Val3);
    }

    [Fact]
    public void SC_TRUESIGHT_uses_val2_10_times_val1_for_Cri()
    {
        // rAthena: val2 = 10*val1 (stored crit, +val1 display per level).
        var pc = MakePc(str: 50, agi: 50, vit: 50, intStat: 50, dex: 50, luk: 50);
        pc.Stats.Cri = 0;
        pc.Stats.Hit = 0;
        var h = new StatusEffectRegistry().Get(StatusType.Truesight)!;
        var sc = new StatusChange { Type = StatusType.Truesight, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(50, sc.Val2); // 10*5
        Assert.Equal(15, sc.Val3); // 3*5
        Assert.Equal(55, pc.Stats.Str); // +5 flat
        Assert.Equal(50, pc.Stats.Cri); // +50 stored
        Assert.Equal(15, pc.Stats.Hit);
        h.OnEnd(pc, sc);
        Assert.Equal(50, pc.Stats.Str);
        Assert.Equal(0, pc.Stats.Cri);
        Assert.Equal(0, pc.Stats.Hit);
    }

    private static PlayerEntity MakePc(short str, short agi, short vit, short intStat, short dex, short luk)
    {
        var pc = new PlayerEntity(1, 1, "P1", Guid.NewGuid(), mapId: 0, x: 0, y: 0);
        pc.Stats.Str = str;
        pc.Stats.Agi = agi;
        pc.Stats.Vit = vit;
        pc.Stats.IntStat = intStat;
        pc.Stats.Dex = dex;
        pc.Stats.Luk = luk;
        return pc;
    }

    private static PlayerEntity MakePcWithBatkDef(ushort batk, short def)
    {
        var pc = new PlayerEntity(1, 1, "P1", Guid.NewGuid(), mapId: 0, x: 0, y: 0);
        pc.Stats.Batk = batk;
        pc.Stats.Def = def;
        return pc;
    }
}
