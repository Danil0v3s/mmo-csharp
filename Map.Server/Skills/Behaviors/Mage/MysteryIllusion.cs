using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>AG_MYSTERY_ILLUSION — Arch Mage Mystery Illusion. Ground unit; ratio +(-100+950*lv+5*SPL).</summary>
public sealed class MysteryIllusion : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public MysteryIllusion() : base(SkillIds.AG_MYSTERY_ILLUSION) { }
    public MysteryIllusion(ISkillUnitService? units = null) : base(SkillIds.AG_MYSTERY_ILLUSION) => _units = units;
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 950 * skillLevel) + 5 * src.Stats.Spl;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
