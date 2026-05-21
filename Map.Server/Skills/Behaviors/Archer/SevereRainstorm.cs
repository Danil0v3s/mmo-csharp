using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_SEVERE_RAINSTORM — Minstrel/Wanderer Severe Rainstorm. Manual
/// port of <c>rathena-fork/src/map/skills/archer/severerainstorm.cpp</c>.
/// Drops a damage-trap ground unit. Equip-lock during duration TODO.
/// </summary>
public sealed class SevereRainstorm : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public SevereRainstorm() : base(SkillIds.WM_SEVERE_RAINSTORM) { }

    public SevereRainstorm(ISkillUnitService? units = null) : base(SkillIds.WM_SEVERE_RAINSTORM)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
