using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_ABYSS_SQUARE — Abyss Square. Manual port of
/// <c>rathena-fork/src/map/skills/thief/abysssquare.cpp</c>.
/// POS2 unit placement; ratio <c>+(-100 + 750*lv) + 5*spl</c>.
/// ABC_MAGIC_SWORD_M partner bonus is TODO.
/// </summary>
public sealed class AbyssSquare : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public AbyssSquare() : base(SkillIds.ABC_ABYSS_SQUARE) { }

    public AbyssSquare(ISkillUnitService? units = null) : base(SkillIds.ABC_ABYSS_SQUARE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 750 * skillLevel) + 5 * src.Stats.Spl;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
