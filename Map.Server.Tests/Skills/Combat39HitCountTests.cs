using Map.Server.Skills;
using Map.Server.Skills.Behaviors.Archer;
using Map.Server.Skills.Behaviors.Swordman;
using Map.Server.Skills.Behaviors.Thief;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-39 — multi-hit hit-count sweep. Every multi-hit weapon-skill plugin now
/// sources its rAthena <c>skill_db</c> <c>HitCount</c> from the
/// <see cref="SkillHitCounts"/> table via the <c>WeaponSkillImpl.GetMultiHitCount</c>
/// default (display = magnitude; the sign is kept for COMBAT-60).
/// </summary>
public class Combat39HitCountTests
{
    [Theory]
    // scalar entries (signed, verbatim from db/re/skill_db.yml)
    [InlineData(SkillIds.TF_DOUBLE, 1, 2)]
    [InlineData(SkillIds.AS_SONICBLOW, 5, -8)]
    [InlineData(SkillIds.MO_TRIPLEATTACK, 5, -3)]
    [InlineData(SkillIds.KN_PIERCE, 5, 3)]
    [InlineData(SkillIds.CG_ARROWVULCAN, 5, -9)]
    [InlineData(SkillIds.RA_AIMEDBOLT, 5, 5)]
    [InlineData(SkillIds.PA_SHIELDCHAIN, 5, 5)]
    [InlineData(SkillIds.NW_MAGAZINE_FOR_ONE, 5, 6)]
    // per-level entries
    [InlineData(SkillIds.DK_STORMSLASH, 1, 1)]
    [InlineData(SkillIds.DK_STORMSLASH, 5, 5)]
    [InlineData(SkillIds.CH_CHAINCRUSH, 1, -1)]
    [InlineData(SkillIds.CH_CHAINCRUSH, 10, -5)]
    [InlineData(SkillIds.CR_ACIDDEMONSTRATION, 7, 7)]
    [InlineData(SkillIds.NPC_COMBOATTACK, 10, -11)]
    public void Table_returns_signed_rathena_hit_count(ushort skillId, ushort level, int expected)
        => Assert.Equal(expected, SkillHitCounts.Get(skillId, level));

    [Fact]
    public void Skill_without_a_row_defaults_to_one()
        => Assert.Equal(1, SkillHitCounts.Get(SkillIds.SM_BASH, 1));

    [Fact]
    public void Per_level_count_clamps_above_the_table_length()
        => Assert.Equal(5, SkillHitCounts.Get(SkillIds.DK_STORMSLASH, 99)); // clamps to last

    // ---- the GetMultiHitCount default routes through the table (magnitude) ----

    [Fact]
    public void DoubleAttack_plugin_reports_two_hits()
        => Assert.Equal(2, new DoubleAttack().GetMultiHitCount(skillLevel: 1));

    [Fact]
    public void VulcanArrow_plugin_reports_nine_hits_magnitude()
        => Assert.Equal(9, new VulcanArrow().GetMultiHitCount(skillLevel: 5)); // |-9|

    [Fact]
    public void Pierce_plugin_base_is_three_hits()
        => Assert.Equal(3, new Pierce().GetMultiHitCount(skillLevel: 5));
}
