using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_ETERNALCHAOS — Bard Down Tempo (Eternal Chaos). Manual port of
/// <c>rathena-fork/src/map/skills/archer/downtempo.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class DownTempo : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public DownTempo() : base(SkillIds.BD_ETERNALCHAOS) { }

    public DownTempo(ISkillUnitService? units = null) : base(SkillIds.BD_ETERNALCHAOS)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
