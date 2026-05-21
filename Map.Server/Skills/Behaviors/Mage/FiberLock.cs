using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>PF_SPIDERWEB — Professor Fiber Lock / Spider Web. Ground unit placement.</summary>
public sealed class FiberLock : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public FiberLock() : base(SkillIds.PF_SPIDERWEB) { }
    public FiberLock(ISkillUnitService? units = null) : base(SkillIds.PF_SPIDERWEB) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
