using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_TATAMIGAESHI — Improvised Defense. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/improviseddefense.cpp</c>.
/// Renewal: 2*(10*lv) ratio (caster-centred unit + self SC). Self SC is TODO.
/// </summary>
public sealed class ImprovisedDefense : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ImprovisedDefense() : base(SkillIds.NJ_TATAMIGAESHI) { }

    public ImprovisedDefense(ISkillUnitService? units = null) : base(SkillIds.NJ_TATAMIGAESHI)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (10 * skillLevel) * 2;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, src.X, src.Y);
}
