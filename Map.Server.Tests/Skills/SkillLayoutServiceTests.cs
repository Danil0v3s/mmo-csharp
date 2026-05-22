using Map.Server.Skills;

namespace Map.Server.Tests.Skills;

/// <summary>
/// SK.100-1b/d — verifies the per-skill layout matrix returns the
/// right cell offsets for named shapes (FireWall row, IceWall cross,
/// WallOfThorn ring, FireBall plus) and falls back to a square radius
/// for skills without a named layout.
/// </summary>
public class SkillLayoutServiceTests
{
    [Fact]
    public void FireWall_ReturnsHorizontalRow()
    {
        var svc = new SkillLayoutService();
        var cells = svc.GetLayoutForSkill(SkillIds.MG_FIREWALL, skillLevel: 5,
            defaultRadius: 0, casterDir: 1 /* odd → horizontal */);
        Assert.Equal(5, cells.Count);
        // All cells share the same Y (= 0), Dx spans -2..2.
        Assert.All(cells, c => Assert.Equal((short)0, c.Dy));
        var xs = cells.Select(c => (int)c.Dx).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { -2, -1, 0, 1, 2 }, xs);
    }

    [Fact]
    public void FireWall_VerticalWhenFacingEvenDir()
    {
        var svc = new SkillLayoutService();
        var cells = svc.GetLayoutForSkill(SkillIds.MG_FIREWALL, skillLevel: 5,
            defaultRadius: 0, casterDir: 0 /* even → vertical */);
        Assert.Equal(5, cells.Count);
        Assert.All(cells, c => Assert.Equal((short)0, c.Dx));
    }

    [Fact]
    public void IceWall_ReturnsCrossWithArm2()
    {
        var svc = new SkillLayoutService();
        var cells = svc.GetLayoutForSkill(SkillIds.WZ_ICEWALL, skillLevel: 1,
            defaultRadius: 0);
        // 1 center + 4 arms * 2 cells = 9 cells.
        Assert.Equal(9, cells.Count);
        Assert.Contains((((short)0, (short)0)), cells);
        Assert.Contains((((short)2, (short)0)), cells);
        Assert.Contains((((short)0, (short)-2)), cells);
    }

    [Fact]
    public void WallOfThorn_ReturnsHollowRing()
    {
        var svc = new SkillLayoutService();
        var cells = svc.GetLayoutForSkill(SkillIds.GN_WALLOFTHORN, skillLevel: 5,
            defaultRadius: 0);
        // 3x3 minus center = 8 cells.
        Assert.Equal(8, cells.Count);
        Assert.DoesNotContain((((short)0, (short)0)), cells);
        Assert.Contains((((short)1, (short)0)), cells);
        Assert.Contains((((short)-1, (short)-1)), cells);
    }

    [Fact]
    public void FireBall_ReturnsCenterPlusCross()
    {
        var svc = new SkillLayoutService();
        var cells = svc.GetLayoutForSkill(SkillIds.MG_FIREBALL, skillLevel: 10,
            defaultRadius: 0);
        // Center + 4 arm cells = 5.
        Assert.Equal(5, cells.Count);
    }

    [Fact]
    public void UnknownSkill_FallsBackToSquareRadius()
    {
        var svc = new SkillLayoutService();
        var cells = svc.GetLayoutForSkill(skillId: 9999, skillLevel: 1,
            defaultRadius: 2);
        // 5x5 = 25 cells.
        Assert.Equal(25, cells.Count);
    }

    [Fact]
    public void DefaultRadius_Zero_ReturnsSingleCell()
    {
        var svc = new SkillLayoutService();
        var cells = svc.GetLayoutForSkill(skillId: 9999, skillLevel: 1,
            defaultRadius: 0);
        Assert.Single(cells);
        Assert.Equal(((short)0, (short)0), cells[0]);
    }
}
