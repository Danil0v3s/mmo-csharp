using System;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Status;

/// <summary>
/// COMBAT-71 — the remaining renewal <c>status_calc_aspd(false)</c> rate-term debuffs
/// (status.cpp:8056-8092) folded into <see cref="StatusCalcService.ComputeScAspd"/>: Ensemble
/// Fatigue, Joint Beat (wrist/knee), Freezing, HallucinationWalk-postdelay, Paralyse, Body
/// Paint, Invisibility, Groomy. Each is asserted at its rAthena-exact rate value.
/// </summary>
public class Combat71AspdDebuffScTests
{
    [Fact]
    public void Freezing_reduces_rate_by_30()
        => Assert.Equal(-30, Rate((sc, pc) => sc.Start(pc, StatusType.Freezing, 1, 0, 0, 0, 60_000)));

    [Fact]
    public void JointBeat_wrist_knee_and_both()
    {
        // BREAK_WRIST 0x02 → −25, BREAK_KNEE 0x04 → −10 (status.hpp:2909-2911).
        Assert.Equal(-25, Rate((sc, pc) => sc.Start(pc, StatusType.Jointbeat, 1, 0x02, 0, 0, 60_000)));
        Assert.Equal(-10, Rate((sc, pc) => sc.Start(pc, StatusType.Jointbeat, 1, 0x04, 0, 0, 60_000)));
        Assert.Equal(-35, Rate((sc, pc) => sc.Start(pc, StatusType.Jointbeat, 1, 0x06, 0, 0, 60_000)));
        // BREAK_ANKLE (0x01) alone is a move-speed break, not ASPD → no rate change.
        Assert.Equal(0, Rate((sc, pc) => sc.Start(pc, StatusType.Jointbeat, 1, 0x01, 0, 0, 60_000)));
    }

    [Fact]
    public void EnsembleFatigue_is_val2_over_10()
        => Assert.Equal(-5, Rate((sc, pc) => sc.Start(pc, StatusType.Ensemblefatigue, 1, 50, 0, 0, 60_000)));

    [Fact]
    public void BodyPaint_is_5_per_val1()
        => Assert.Equal(-15, Rate((sc, pc) => sc.Start(pc, StatusType.Bodypaint, 3, 0, 0, 0, 60_000)));

    [Fact]
    public void Paralyse_only_reduces_when_val3_is_one()
    {
        Assert.Equal(-10, Rate((sc, pc) => sc.Start(pc, StatusType.Paralyse, 1, 0, 1, 0, 60_000)));
        Assert.Equal(0, Rate((sc, pc) => sc.Start(pc, StatusType.Paralyse, 1, 0, 0, 0, 60_000)));
    }

    [Fact]
    public void HallucinationWalk_postdelay_invisibility_groomy()
    {
        Assert.Equal(-50, Rate((sc, pc) => sc.Start(pc, StatusType.HallucinationwalkPostdelay, 1, 0, 0, 0, 60_000)));
        Assert.Equal(-7, Rate((sc, pc) => sc.Start(pc, StatusType.Invisibility, 1, 7, 0, 0, 60_000)));
        Assert.Equal(-3, Rate((sc, pc) => sc.Start(pc, StatusType.Groomy, 1, 3, 0, 0, 60_000)));
    }

    [Fact]
    public void Remaining_rate_positives_and_negatives()
    {
        Assert.Equal(5, Rate((sc, pc) => sc.Start(pc, StatusType.IncreaseAgi, 5, 0, 0, 0, 60_000)));     // +val1
        Assert.Equal(9, Rate((sc, pc) => sc.Start(pc, StatusType.StarComfort, 3, 0, 0, 0, 60_000)));     // 3*val1
        Assert.Equal(20, Rate((sc, pc) => sc.Start(pc, StatusType.Nibelungen, 1, 1, 0, 0, 60_000)));     // val2==RINGNBL_ASPDRATE → +20
        Assert.Equal(0, Rate((sc, pc) => sc.Start(pc, StatusType.Nibelungen, 1, 2, 0, 0, 60_000)));      // other ring → no aspd
        Assert.Equal(-4, Rate((sc, pc) => sc.Start(pc, StatusType.Gloomyday, 1, 0, 4, 0, 60_000)));      // −val3 (debuff)
    }

    // ---- end-to-end: the rate debuff slows the scheduled attack delay ----

    [Fact]
    public void Freezing_slows_the_attack_delay_end_to_end()
    {
        var baseAdelay = Adelay((sc, pc) => { });
        var frozen = Adelay((sc, pc) => sc.Start(pc, StatusType.Freezing, 1, 0, 0, 0, 60_000));
        Assert.True(frozen > baseAdelay);
    }

    // ---- helpers ----

    private static int Rate(Action<RecordingStatusChangeService, PlayerEntity> arrange)
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        arrange(sc, pc);
        var calc = new StatusCalcService(sc: new Lazy<IStatusChangeService>(() => sc));
        return calc.ComputeScAspd(pc).rateSc;
    }

    private static int Adelay(Action<RecordingStatusChangeService, PlayerEntity> arrange)
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        arrange(sc, pc);
        var calc = new StatusCalcService(sc: new Lazy<IStatusChangeService>(() => sc));
        calc.CalcPc(pc, new PcBaseInputs(
            BaseLevel: 99, JobLevel: 50,
            Str: 1, Agi: 90, Vit: 1, Int: 1, Dex: 50, Luk: 1,
            Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
            WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 0, EquipMdef: 0,
            AttackRange: 1, WeaponLevel: 0, WeaponType: 1));
        return pc.Stats.Adelay;
    }
}
