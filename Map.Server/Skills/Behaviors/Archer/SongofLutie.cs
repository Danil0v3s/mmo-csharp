using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_APPLEIDUN — Bard Song of Lutie (Apple of Idun). Manual port of
/// <c>rathena-fork/src/map/skills/archer/songoflutie.cpp</c>.
/// Drops the song ground unit.
/// </summary>
public sealed class SongofLutie : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public SongofLutie() : base(SkillIds.BA_APPLEIDUN) { }

    public SongofLutie(ISkillUnitService? units = null) : base(SkillIds.BA_APPLEIDUN)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
