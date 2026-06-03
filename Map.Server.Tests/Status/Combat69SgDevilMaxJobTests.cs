using System;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Status;
using WT = Map.Server.Inventory.WeaponTypeCodes;

namespace Map.Server.Tests.Status;

/// <summary>
/// COMBAT-69 — the SG_DEVIL ASPD `val` gate's <c>|| pc_is_maxjoblv</c> half (status.cpp:2345).
/// A Star Gladiator (Taekwon 2nd-class) at their job-level cap with SG_DEVIL learned gets the
/// <c>+1 + lv</c> bonus; below the cap, nothing. Star Emperors (3rd-class) keep the bonus
/// regardless of job level.
/// </summary>
public class Combat69SgDevilMaxJobTests
{
    [Fact]
    public void StarGladiator_at_max_job_level_gets_the_bonus()
    {
        var pc = NewPc();
        pc.ClassMask = MapidClass.StarGladiator; // Taekwon | Upper — NOT a Star Emperor
        pc.LearnedSkills[SkillIds.SG_DEVIL] = 3;

        pc.JobLevel = 50; // at cap (maxJobLevel 50) → 1 + 3
        Assert.Equal(4, StatusCalcService.ComputeSkillAspdVal(pc, WT.Fist, maxJobLevel: 50));

        pc.JobLevel = 49; // below cap → no bonus
        Assert.Equal(0, StatusCalcService.ComputeSkillAspdVal(pc, WT.Fist, maxJobLevel: 50));
    }

    [Fact]
    public void StarEmperor_keeps_the_bonus_below_max_job_level()
    {
        var pc = NewPc();
        pc.ClassMask = MapidClass.StarGladiator | MapidClass.ThirdClass; // Star Emperor
        pc.LearnedSkills[SkillIds.SG_DEVIL] = 3;
        pc.JobLevel = 1;

        // IsStarEmperor → bonus even with no max-job input.
        Assert.Equal(4, StatusCalcService.ComputeSkillAspdVal(pc, WT.Fist, maxJobLevel: 0));
    }

    [Fact]
    public void StarGladiator_without_max_job_input_gets_no_bonus()
    {
        // Backward-compat: a Star Gladiator at max job but no job-stats cache wired
        // (maxJobLevel defaults 0) keeps the prior Star-Emperor-only behavior.
        var pc = NewPc();
        pc.ClassMask = MapidClass.StarGladiator;
        pc.LearnedSkills[SkillIds.SG_DEVIL] = 3;
        pc.JobLevel = 50;

        Assert.Equal(0, StatusCalcService.ComputeSkillAspdVal(pc, WT.Fist));
    }

    private static PlayerEntity NewPc() => new(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
}
