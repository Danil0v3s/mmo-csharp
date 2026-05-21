using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_DESPERADO — Gunslinger Desperado. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/desperado.cpp</c>.
/// Ratio <c>+50*(lv-1)</c>; ×2 with SC_FALLEN_ANGEL active.
/// CastendPos2 drops the splash unit at (x, y).
/// </summary>
public sealed class Desperado : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Desperado() : base(SkillIds.GS_DESPERADO) { }

    public Desperado(ISkillUnitService? units = null) : base(SkillIds.GS_DESPERADO)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 * (skillLevel - 1);
    // TODO: doubles under SC_FALLEN_ANGEL — needs SC plumbing into ratio.

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
