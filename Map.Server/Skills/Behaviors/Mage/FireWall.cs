using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>MG_FIREWALL — Mage Fire Wall. Ground unit placement; ratio -50.</summary>
public sealed class FireWall : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public FireWall() : base(SkillIds.MG_FIREWALL) { }
    public FireWall(ISkillUnitService? units = null) : base(SkillIds.MG_FIREWALL) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio - 50;
}
