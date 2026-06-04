using System;
using System.Linq;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// SC-07 — magnitude audit of the generator-default SCs. The enumeration
/// (<see cref="StatusEffectRegistry.GeneratedStatModDefaultTypes"/>) is the
/// review worklist; this guard keeps it visible so new CalcFlag SCs are
/// triaged rather than silently served the `+Val1` body. Also pins the first
/// converted sign-wrong debuff (Fear).
/// </summary>
public class SC07GeneratorAuditTests
{
    private static readonly StatusEffectRegistry Reg = new();

    [Fact]
    public void GeneratorDefaultSet_IsEnumerated_AndNonTrivial()
    {
        var generated = Reg.GeneratedStatModDefaultTypes;
        // The ~159 generator-default stat-mod SCs (review worklist). A wide
        // bound: this should never silently collapse to ~0 (a regression that
        // would mean every SC lost its stat mod) nor balloon past the CalcFlag
        // table size. Converting SCs to explicit bodies lowers it over time.
        Assert.InRange(generated.Count, 80, 360);
        Assert.Equal(generated.Count, generated.Distinct().Count()); // no dup records
    }

    [Fact]
    public void Fear_IsConverted_NotGeneratorDefault()
    {
        // Fear now has an explicit body (20% Hit/Flee reduction), so it must
        // have left the generator-default worklist.
        Assert.DoesNotContain(StatusType.Fear, Reg.GeneratedStatModDefaultTypes);
    }

    [Theory]
    // SC-MAGNITUDE — SCs converted by the post-generator override waves (Wave 32/60/61) must be pruned
    // from the worklist. Before the prune they were over-reported as still-default (their override runs
    // AFTER the generator added them). Each of these has a real rAthena Val2/Val3 magnitude handler.
    [InlineData(StatusType.Fortune)]      // val2 = val1*10 (Cri)
    [InlineData(StatusType.Whistle)]      // val2 = 18+2*val1 (Flee), val3 = (val1+1)/2 (Flee2)
    [InlineData(StatusType.Humming)]      // val2 = 4*val1 (Hit)
    [InlineData(StatusType.Dontforgetme)] // val2 = 1+30*val1 (Aspd), val3 = 5+2*val1 (Speed)
    [InlineData(StatusType.Assncros)]     // val2 = val1<10 ? val1*2-1 : 20 (AspdRate)
    [InlineData(StatusType.Truesight)]    // val2 = 10*val1 (Cri), val3 = 3*val1 (Hit)
    public void OverriddenSc_IsPrunedFromGeneratorDefaultWorklist(StatusType type)
        => Assert.DoesNotContain(type, Reg.GeneratedStatModDefaultTypes);

    // ---- Fear: fixed 20% Hit + Flee REDUCTION (not +Val1) ----

    [Fact]
    public void Fear_Reduces_Hit_And_Flee_By20Percent()
    {
        var pc = new PlayerEntity(1, 1, "P", Guid.NewGuid(), 0, 0, 0);
        pc.Stats.Hit = 200; pc.Stats.Flee = 150;
        var sc = new StatusChange { Type = StatusType.Fear, Val1 = 5 };
        Reg.Get(StatusType.Fear)!.OnStart(pc, sc, null);

        Assert.Equal(160, pc.Stats.Hit);   // 200 - 20%
        Assert.Equal(120, pc.Stats.Flee);  // 150 - 20%

        Reg.Get(StatusType.Fear)!.OnEnd(pc, sc);
        Assert.Equal(200, pc.Stats.Hit);   // reverts cleanly
        Assert.Equal(150, pc.Stats.Flee);
    }
}
