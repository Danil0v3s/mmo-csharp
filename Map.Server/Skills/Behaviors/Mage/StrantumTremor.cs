using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>AG_STRANTUM_TREMOR — Arch Mage Strantum Tremor. Ground unit; ratio +(-100+100+730*lv+5*SPL).</summary>
public sealed class StrantumTremor : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public StrantumTremor() : base(SkillIds.AG_STRANTUM_TREMOR) { }
    public StrantumTremor(ISkillUnitService? units = null) : base(SkillIds.AG_STRANTUM_TREMOR) => _units = units;
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 100 + 730 * skillLevel) + 5 * src.Stats.Spl;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
