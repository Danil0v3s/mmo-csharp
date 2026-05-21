using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_CROSS_RAIN — Imperial Guard Cross Rain. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/crossrain.cpp</c>.
/// Drops a damage unit at (x, y). Ratio scales differently with SC_HOLY_S
/// and IG_SPEAR_SWORD_M skill level; for now we apply the basic 450*lv
/// + 7*SPL formula. Holy-S bonus + IG_SPEAR_SWORD_M skill-tree lookup are TODO.
/// </summary>
public sealed class CrossRain : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CrossRain() : base(SkillIds.IG_CROSS_RAIN) { }

    public CrossRain(ISkillUnitService? units = null) : base(SkillIds.IG_CROSS_RAIN)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 450 * skillLevel) + 7 * src.Stats.Spl;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
