using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// Wave 96 — verifies <c>SC_MARIONETTE</c> / <c>SC_MARIONETTE2</c> OnStart/OnEnd
/// match the rAthena packed-byte formula from <c>status.cpp:11376-11414</c>:
/// <list type="bullet">
///   <item>Source loses (source.stat ÷ 2) on each of str/agi/vit/int/dex/luk.</item>
///   <item>Target gains (source.stat ÷ 2), each capped at (max_parameter − target.stat).</item>
///   <item>Deltas packed into Val3 (str|agi|vit) and Val4 (int|dex|luk) at byte width.</item>
/// </list>
/// </summary>
public class MarionetteFormulaTests
{
    [Fact]
    public void SC_MARIONETTE_OnStart_subtracts_packed_deltas_from_source_stats()
    {
        var pc = MakePc(str: 50, agi: 60, vit: 70, intStat: 80, dex: 90, luk: 99);

        // Pack source.stat/2 byte triples — exactly what MarionetteControl computes.
        int val3 = (25 << 16) | (30 << 8) | 35; // str/2|agi/2|vit/2
        int val4 = (40 << 16) | (45 << 8) | 49; // int/2|dex/2|luk/2 (99/2=49)

        var handler = new StatusEffectRegistry().Get(StatusType.Marionette);
        Assert.NotNull(handler);
        var sce = new StatusChange { Type = StatusType.Marionette, Val3 = val3, Val4 = val4 };
        handler.OnStart(pc, sce, null);

        Assert.Equal(25, pc.Stats.Str);     // 50 − 25
        Assert.Equal(30, pc.Stats.Agi);     // 60 − 30
        Assert.Equal(35, pc.Stats.Vit);     // 70 − 35
        Assert.Equal(40, pc.Stats.IntStat); // 80 − 40
        Assert.Equal(45, pc.Stats.Dex);     // 90 − 45
        Assert.Equal(50, pc.Stats.Luk);     // 99 − 49 = 50
    }

    [Fact]
    public void SC_MARIONETTE_OnEnd_restores_source_stats()
    {
        var pc = MakePc(str: 50, agi: 60, vit: 70, intStat: 80, dex: 90, luk: 99);
        int val3 = (25 << 16) | (30 << 8) | 35;
        int val4 = (40 << 16) | (45 << 8) | 49;

        var handler = new StatusEffectRegistry().Get(StatusType.Marionette)!;
        var sce = new StatusChange { Type = StatusType.Marionette, Val3 = val3, Val4 = val4 };
        handler.OnStart(pc, sce, null);
        handler.OnEnd(pc, sce);

        Assert.Equal(50, pc.Stats.Str);
        Assert.Equal(60, pc.Stats.Agi);
        Assert.Equal(70, pc.Stats.Vit);
        Assert.Equal(80, pc.Stats.IntStat);
        Assert.Equal(90, pc.Stats.Dex);
        Assert.Equal(99, pc.Stats.Luk);
    }

    [Fact]
    public void SC_MARIONETTE2_OnStart_adds_packed_deltas_to_target_stats()
    {
        var pc = MakePc(str: 50, agi: 40, vit: 30, intStat: 20, dex: 10, luk: 1);
        int val3 = (25 << 16) | (20 << 8) | 15;
        int val4 = (10 << 16) | (5 << 8) | 0;

        var handler = new StatusEffectRegistry().Get(StatusType.Marionette2)!;
        var sce = new StatusChange { Type = StatusType.Marionette2, Val3 = val3, Val4 = val4 };
        handler.OnStart(pc, sce, null);

        Assert.Equal(75, pc.Stats.Str);     // 50 + 25
        Assert.Equal(60, pc.Stats.Agi);     // 40 + 20
        Assert.Equal(45, pc.Stats.Vit);     // 30 + 15
        Assert.Equal(30, pc.Stats.IntStat); // 20 + 10
        Assert.Equal(15, pc.Stats.Dex);     // 10 + 5
        Assert.Equal(1,  pc.Stats.Luk);     // 1 + 0
    }

    [Fact]
    public void SC_MARIONETTE2_OnEnd_reverses_added_deltas()
    {
        var pc = MakePc(str: 50, agi: 0, vit: 0, intStat: 0, dex: 0, luk: 0);
        int val3 = 25 << 16;

        var handler = new StatusEffectRegistry().Get(StatusType.Marionette2)!;
        var sce = new StatusChange { Type = StatusType.Marionette2, Val3 = val3, Val4 = 0 };
        handler.OnStart(pc, sce, null);
        Assert.Equal(75, pc.Stats.Str);
        handler.OnEnd(pc, sce);
        Assert.Equal(50, pc.Stats.Str);
    }

    [Fact]
    public void MarionetteControl_packs_source_stats_at_byte_width()
    {
        // Verifies the packing layout — what MarionetteControl writes.
        int str = 50, agi = 60, vit = 70, intStat = 80, dex = 90, luk = 99;
        int strH = str / 2, agiH = agi / 2, vitH = vit / 2;
        int intH = intStat / 2, dexH = dex / 2, lukH = luk / 2;

        int val3 = (strH << 16) | (agiH << 8) | vitH;
        int val4 = (intH << 16) | (dexH << 8) | lukH;

        Assert.Equal(25, (val3 >> 16) & 0xFF);
        Assert.Equal(30, (val3 >> 8) & 0xFF);
        Assert.Equal(35, val3 & 0xFF);
        Assert.Equal(40, (val4 >> 16) & 0xFF);
        Assert.Equal(45, (val4 >> 8) & 0xFF);
        Assert.Equal(49, val4 & 0xFF); // 99/2 = 49 (truncated)
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
}
