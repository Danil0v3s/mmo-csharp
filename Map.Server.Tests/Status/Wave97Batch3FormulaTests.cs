using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// Wave 97-3 — pins the rAthena formulas for the third batch: Drumbattle,
/// DontForgetMe, Fortune, Gloria, Spirit (SL_HIGH branch).
/// </summary>
public class Wave97Batch3FormulaTests
{
    [Fact]
    public void SC_DRUMBATTLE_adds_flat_watk_and_def()
    {
        // rAthena: val2 = 15+5*val1 (Watk flat), val3 = 15*val1 (Def flat).
        // status_calc_watk:7344-7345 + status_calc_def:7773-7774.
        var pc = MakePc();
        pc.Stats.WatkMin = 50;
        pc.Stats.WatkMax = 60;
        pc.Stats.Def = 30;
        var h = new StatusEffectRegistry().Get(StatusType.Drumbattle)!;
        var sc = new StatusChange { Type = StatusType.Drumbattle, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(40, sc.Val2);  // 15+25
        Assert.Equal(75, sc.Val3);  // 15*5
        Assert.Equal(90, pc.Stats.WatkMin);
        Assert.Equal(100, pc.Stats.WatkMax);
        Assert.Equal(105, pc.Stats.Def);
        h.OnEnd(pc, sc);
        Assert.Equal(50, pc.Stats.WatkMin);
        Assert.Equal(60, pc.Stats.WatkMax);
        Assert.Equal(30, pc.Stats.Def);
    }

    [Fact]
    public void SC_DONTFORGETME_slows_aspd()
    {
        // rAthena val2 = 1+30*val1 ASPD penalty; consumer aspd_rate
        // bonus -= val2/10.  Project AspdRate = higher = faster, so we
        // SUBTRACT val2/10 from AspdRate.
        var pc = MakePc();
        pc.Stats.AspdRate = 200;
        var h = new StatusEffectRegistry().Get(StatusType.Dontforgetme)!;
        var sc = new StatusChange { Type = StatusType.Dontforgetme, Val1 = 5 };
        h.OnStart(pc, sc, null);
        // val2 = 1+30*5 = 151; aspdSlow = 151/10 = 15.
        Assert.Equal(151, sc.Val2);
        Assert.Equal(185, pc.Stats.AspdRate);
        h.OnEnd(pc, sc);
        Assert.Equal(200, pc.Stats.AspdRate);
    }

    [Fact]
    public void SC_FORTUNE_adds_val1_times_10_to_Cri()
    {
        // rAthena val2 = 10*val1 (stored crit; +val1 display per level).
        var pc = MakePc();
        pc.Stats.Cri = 100;
        var h = new StatusEffectRegistry().Get(StatusType.Fortune)!;
        var sc = new StatusChange { Type = StatusType.Fortune, Val1 = 5 };
        h.OnStart(pc, sc, null);
        Assert.Equal(50, sc.Val2);     // 10*5
        Assert.Equal(150, pc.Stats.Cri); // 100+50
        h.OnEnd(pc, sc);
        Assert.Equal(100, pc.Stats.Cri);
    }

    [Fact]
    public void SC_GLORIA_adds_flat_30_Luk_regardless_of_Val1()
    {
        // rAthena status_calc_luk:7128-7129: `luk += 30` always.
        var pc = MakePc(luk: 50);
        var h = new StatusEffectRegistry().Get(StatusType.Gloria)!;
        // Val1=1, but consumer ignores val1 and adds flat 30.
        var sc1 = new StatusChange { Type = StatusType.Gloria, Val1 = 1 };
        h.OnStart(pc, sc1, null);
        Assert.Equal(80, pc.Stats.Luk);
        h.OnEnd(pc, sc1);
        Assert.Equal(50, pc.Stats.Luk);

        // Val1=5 same result.
        var sc5 = new StatusChange { Type = StatusType.Gloria, Val1 = 5 };
        h.OnStart(pc, sc5, null);
        Assert.Equal(80, pc.Stats.Luk);
        h.OnEnd(pc, sc5);
        Assert.Equal(50, pc.Stats.Luk);
    }

    [Fact]
    public void SC_SPIRIT_SL_HIGH_branch_applies_packed_stat_halves()
    {
        // rAthena SL_HIGH spirit-link: val3 packs str/agi/vit at byte width,
        // val4 packs int/dex/luk.  Consumer status_calc_str:6786-6787 reads
        // (val3>>16)&0xFF, etc.  Val2 = SL_HIGH = 25.
        var pc = MakePc(str: 10, agi: 20, vit: 30, intStat: 40, dex: 50, luk: 60);
        var h = new StatusEffectRegistry().Get(StatusType.Spirit)!;
        int val3 = (5 << 16) | (8 << 8) | 12; // str=5, agi=8, vit=12 halves
        int val4 = (20 << 16) | (25 << 8) | 30; // int=20, dex=25, luk=30
        var sc = new StatusChange { Type = StatusType.Spirit, Val1 = 1, Val2 = 25, Val3 = val3, Val4 = val4 };
        h.OnStart(pc, sc, null);
        Assert.Equal(15, pc.Stats.Str);     // 10+5
        Assert.Equal(28, pc.Stats.Agi);
        Assert.Equal(42, pc.Stats.Vit);
        Assert.Equal(60, pc.Stats.IntStat);
        Assert.Equal(75, pc.Stats.Dex);
        Assert.Equal(90, pc.Stats.Luk);
        h.OnEnd(pc, sc);
        Assert.Equal(10, pc.Stats.Str);
        Assert.Equal(20, pc.Stats.Agi);
        Assert.Equal(30, pc.Stats.Vit);
        Assert.Equal(40, pc.Stats.IntStat);
        Assert.Equal(50, pc.Stats.Dex);
        Assert.Equal(60, pc.Stats.Luk);
    }

    [Fact]
    public void SC_SPIRIT_non_SL_HIGH_falls_back_to_Val1_add()
    {
        // Non-SL_HIGH links (Val2 != 25) use the generator's +Val1 fallback
        // so the CalcFlag stat-mod gate stays satisfied.
        var pc = MakePc(str: 10, agi: 10, vit: 10, intStat: 10, dex: 10, luk: 10);
        var h = new StatusEffectRegistry().Get(StatusType.Spirit)!;
        var sc = new StatusChange { Type = StatusType.Spirit, Val1 = 5, Val2 = 10 /* SL_KNIGHT or similar */ };
        h.OnStart(pc, sc, null);
        Assert.Equal(15, pc.Stats.Str);
        Assert.Equal(15, pc.Stats.Agi);
        h.OnEnd(pc, sc);
        Assert.Equal(10, pc.Stats.Str);
    }

    private static PlayerEntity MakePc(short str = 0, short agi = 0, short vit = 0, short intStat = 0, short dex = 0, short luk = 0)
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
}
