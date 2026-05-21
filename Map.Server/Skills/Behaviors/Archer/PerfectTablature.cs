using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_WHISTLE — Bard Perfect Tablature (Whistle). Manual port of
/// <c>rathena-fork/src/map/skills/archer/perfecttablature.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class PerfectTablature : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public PerfectTablature() : base(SkillIds.BA_WHISTLE) { }

    public PerfectTablature(ISkillUnitService? units = null) : base(SkillIds.BA_WHISTLE)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
