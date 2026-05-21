using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// CG_MOONLIT — Clown/Gypsy Sheltering Bliss (Moonlit Petals Water).
/// Manual port of <c>rathena-fork/src/map/skills/archer/shelteringbliss.cpp</c>.
/// Drops the song ground unit.
/// </summary>
public sealed class ShelteringBliss : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ShelteringBliss() : base(SkillIds.CG_MOONLIT) { }

    public ShelteringBliss(ISkillUnitService? units = null) : base(SkillIds.CG_MOONLIT)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
