using Map.Server.Entities;
using Map.Server.Skills;

namespace Map.Server.Tests.Skills;

/// <summary>
/// SK.100-1c — verifies the per-unit cloaking flag + visibility
/// classifier. Cloaking groups (traps / Pneuma / Lullaby) hide from
/// non-owner observers; non-cloaking groups are visible to all.
/// </summary>
public class SkillUnitVisibilityTests
{
    [Fact]
    public void NonCloakingGroup_VisibleToEveryone()
    {
        var group = MakeGroup(casterId: 1, hidden: false);
        var observer = MakePc(charId: 99);
        Assert.True(SkillUnitVisibility.IsVisibleTo(group, observer));
    }

    [Fact]
    public void CloakingGroup_VisibleToCaster()
    {
        var group = MakeGroup(casterId: 1, hidden: true);
        var caster = MakePc(charId: 1);
        Assert.True(SkillUnitVisibility.IsVisibleTo(group, caster));
    }

    [Fact]
    public void CloakingGroup_HiddenFromStranger()
    {
        var group = MakeGroup(casterId: 1, hidden: true);
        var stranger = MakePc(charId: 99);
        Assert.False(SkillUnitVisibility.IsVisibleTo(group, stranger));
    }

    [Fact]
    public void HiddenFromNonOwner_DefaultsFalse()
    {
        // SkillUnitGroup ctor doesn't require HiddenFromNonOwner; default
        // is false (visible).
        var group = MakeGroup(casterId: 1);
        Assert.False(group.HiddenFromNonOwner);
    }

    private static SkillUnitGroup MakeGroup(int casterId, bool hidden = false)
        => new()
        {
            SkillId = 99,
            SkillLevel = 1,
            CasterId = new EntityId(casterId),
            MapId = 1,
            ExpiresAt = long.MaxValue,
            IntervalMs = 1000,
            HiddenFromNonOwner = hidden,
        };

    private static PlayerEntity MakePc(int charId) =>
        new(charId, charId, $"P{charId}", System.Guid.NewGuid(), 1, 100, 100);
}
