using Map.Server.Skills.Behaviors;

namespace Map.Server.Tests.Skills;

/// <summary>
/// SK.100-2 — verifies SkillBehaviorRegistry.GetOrDefault never
/// returns null, hand-written impls take precedence, and the no-op
/// fallback runs every default SkillImpl virtual without throwing.
/// </summary>
public class SkillBehaviorBulkBackfillTests
{
    [Fact]
    public void GetOrDefault_UnknownSkill_ReturnsNoOpNotNull()
    {
        var reg = new SkillBehaviorRegistry(System.Array.Empty<SkillImpl>());
        var impl = reg.GetOrDefault(skillId: 9999);
        Assert.NotNull(impl);
    }

    [Fact]
    public void Get_UnknownSkill_ReturnsNull()
    {
        // Strict lookup still returns null so call sites that
        // differentiate "bespoke" vs "default" stay correct.
        var reg = new SkillBehaviorRegistry(System.Array.Empty<SkillImpl>());
        Assert.Null(reg.Get(skillId: 9999));
    }

    [Fact]
    public void HasCustomImpl_FalseForUnknown_TrueForRegistered()
    {
        var custom = new TestImpl(skillId: 42);
        var reg = new SkillBehaviorRegistry(new SkillImpl[] { custom });
        Assert.True(reg.HasCustomImpl(42));
        Assert.False(reg.HasCustomImpl(9999));
    }

    [Fact]
    public void GetOrDefault_RegisteredSkill_ReturnsHandWrittenImpl()
    {
        var custom = new TestImpl(skillId: 42);
        var reg = new SkillBehaviorRegistry(new SkillImpl[] { custom });
        Assert.Same(custom, reg.GetOrDefault(42));
    }

    [Fact]
    public void NoOpFallback_DefaultVirtualsRunWithoutThrowing()
    {
        var reg = new SkillBehaviorRegistry(System.Array.Empty<SkillImpl>());
        var impl = reg.GetOrDefault(9999);
        // Pass-through calculations should return the input values
        // unchanged (matches SkillImpl's default virtual bodies).
        Assert.Equal(100, impl.CalculateSkillRatio(100, null!, null!, 1));
        Assert.Equal((short)50, impl.ModifyHitRate(50, null!, null!, 1));
    }

    private sealed class TestImpl : SkillImpl
    {
        public TestImpl(ushort skillId) : base(skillId) { }
    }
}
