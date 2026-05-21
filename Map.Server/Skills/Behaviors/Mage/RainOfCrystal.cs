using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>AG_RAIN_OF_CRYSTAL — Arch Mage Rain Of Crystal. Ground unit; ratio +(-100+180+760*lv+5*SPL).</summary>
public sealed class RainOfCrystal : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public RainOfCrystal() : base(SkillIds.AG_RAIN_OF_CRYSTAL) { }
    public RainOfCrystal(ISkillUnitService? units = null) : base(SkillIds.AG_RAIN_OF_CRYSTAL) => _units = units;
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 180 + 760 * skillLevel) + 5 * src.Stats.Spl;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
