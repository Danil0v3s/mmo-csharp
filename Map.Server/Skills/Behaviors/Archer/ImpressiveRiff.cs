using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_ASSASSINCROSS — Bard Impressive Riff (Assassin Cross of
/// Sunset). Manual port of <c>rathena-fork/src/map/skills/archer/impressiveriff.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class ImpressiveRiff : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ImpressiveRiff() : base(SkillIds.BA_ASSASSINCROSS) { }

    public ImpressiveRiff(ISkillUnitService? units = null) : base(SkillIds.BA_ASSASSINCROSS)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
