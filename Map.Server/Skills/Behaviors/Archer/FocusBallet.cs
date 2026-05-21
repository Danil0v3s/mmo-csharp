using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_HUMMING — Dancer Focus Ballet (Humming). Manual port of
/// <c>rathena-fork/src/map/skills/archer/focusballet.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class FocusBallet : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public FocusBallet() : base(SkillIds.DC_HUMMING) { }

    public FocusBallet(ISkillUnitService? units = null) : base(SkillIds.DC_HUMMING)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
