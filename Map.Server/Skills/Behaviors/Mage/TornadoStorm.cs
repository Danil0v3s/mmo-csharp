using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>AG_TORNADO_STORM — Arch Mage Tornado Storm. Ground unit; ratio +(-100+100+760*lv+5*SPL).</summary>
public sealed class TornadoStorm : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public TornadoStorm() : base(SkillIds.AG_TORNADO_STORM) { }
    public TornadoStorm(ISkillUnitService? units = null) : base(SkillIds.AG_TORNADO_STORM) => _units = units;
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 100 + 760 * skillLevel) + 5 * src.Stats.Spl;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
