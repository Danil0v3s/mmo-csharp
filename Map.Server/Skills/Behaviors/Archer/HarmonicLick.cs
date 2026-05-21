using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_RINGNIBELUNGEN — Bard Harmonic Lick (Ring of Nibelungen).
/// Manual port of <c>rathena-fork/src/map/skills/archer/harmoniclick.cpp</c>.
/// Drops the song unit (legacy path).
/// </summary>
public sealed class HarmonicLick : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public HarmonicLick() : base(SkillIds.BD_RINGNIBELUNGEN) { }

    public HarmonicLick(ISkillUnitService? units = null) : base(SkillIds.BD_RINGNIBELUNGEN)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
