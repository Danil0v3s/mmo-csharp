using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_RICHMANKIM — Bard Mental Sensing (Mr. Kim a Rich Man). Manual
/// port of <c>rathena-fork/src/map/skills/archer/mentalsensing.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class MentalSensing : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public MentalSensing() : base(SkillIds.BD_RICHMANKIM) { }

    public MentalSensing(ISkillUnitService? units = null) : base(SkillIds.BD_RICHMANKIM)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
