using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>WZ_ICEWALL — Wizard Ice Wall. Ground unit placement (blocks line-of-sight).</summary>
public sealed class IceWall : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public IceWall() : base(SkillIds.WZ_ICEWALL) { }
    public IceWall(ISkillUnitService? units = null) : base(SkillIds.WZ_ICEWALL) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
