using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_GROUNDDRIFT — Gunslinger Ground Drift. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/grounddrift.cpp</c>.
/// Renewal ratio <c>+(100 + 20*lv)</c>. Drops the trap unit at the
/// target cell.
/// </summary>
public sealed class GroundDrift : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public GroundDrift() : base(SkillIds.GS_GROUNDDRIFT) { }

    public GroundDrift(ISkillUnitService? units = null) : base(SkillIds.GS_GROUNDDRIFT)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 + 20 * skillLevel;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
