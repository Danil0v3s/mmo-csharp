using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_GRENADES_DROPPING — Night Watch Grenades Dropping. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/grenadesdropping.cpp</c>.
/// Ratio <c>+(-100 + 550 + 850*lv) + 5*CON</c>. Random splash drop loop
/// is TODO; we place a single unit at the cast cell.
/// </summary>
public sealed class GrenadesDropping : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public GrenadesDropping() : base(SkillIds.NW_GRENADES_DROPPING) { }

    public GrenadesDropping(ISkillUnitService? units = null) : base(SkillIds.NW_GRENADES_DROPPING)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 550 + 850 * skillLevel) + 5 * src.Stats.Con;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
