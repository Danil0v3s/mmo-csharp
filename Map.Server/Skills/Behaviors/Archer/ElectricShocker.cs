using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_ELECTRICSHOCKER — Ranger Electric Shocker. Manual port of
/// <c>rathena-fork/src/map/skills/archer/electricshocker.cpp</c>.
/// Drops the trap at the cast XY.
/// </summary>
public sealed class ElectricShocker : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ElectricShocker() : base(SkillIds.RA_ELECTRICSHOCKER) { }

    public ElectricShocker(ISkillUnitService? units = null) : base(SkillIds.RA_ELECTRICSHOCKER)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
