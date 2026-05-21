using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_SERVICEFORYOU — Dancer Gypsy's Kiss (Service for You). Manual
/// port of <c>rathena-fork/src/map/skills/archer/gypsyskiss.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class GypsysKiss : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public GypsysKiss() : base(SkillIds.DC_SERVICEFORYOU) { }

    public GypsysKiss(ISkillUnitService? units = null) : base(SkillIds.DC_SERVICEFORYOU)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
