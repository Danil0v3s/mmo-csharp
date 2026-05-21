using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>AG_FLORAL_FLARE_ROAD — Arch Mage Floral Flare Road. Ground unit placement; ratio +(-100+50+740*lv+5*SPL).</summary>
public sealed class FloralFlareRoad : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public FloralFlareRoad() : base(SkillIds.AG_FLORAL_FLARE_ROAD) { }
    public FloralFlareRoad(ISkillUnitService? units = null) : base(SkillIds.AG_FLORAL_FLARE_ROAD) => _units = units;
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 50 + 740 * skillLevel) + 5 * src.Stats.Spl;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
