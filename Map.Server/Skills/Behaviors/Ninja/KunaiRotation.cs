using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_KUNAIKAITEN — Kunai Rotation. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/kunairotation.cpp</c>.
/// POS2 unit placement; ratio <c>+(-100 + 950 + 1050*lv) + 5*pow</c>.
/// Self-SC + paired SS_KUNAIWAIKYOKU placement TODO.
/// </summary>
public sealed class KunaiRotation : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public KunaiRotation() : base(SkillIds.SS_KUNAIKAITEN) { }

    public KunaiRotation(ISkillUnitService? units = null) : base(SkillIds.SS_KUNAIKAITEN)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 950 + 1050 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
