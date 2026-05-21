using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_INTOABYSS — Bard Power Chord (Into the Abyss). Manual port of
/// <c>rathena-fork/src/map/skills/archer/powerchord.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class PowerChord : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public PowerChord() : base(SkillIds.BD_INTOABYSS) { }

    public PowerChord(ISkillUnitService? units = null) : base(SkillIds.BD_INTOABYSS)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
