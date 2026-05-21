using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_ROKISWEIL — Bard/Dancer Classical Pluck. Manual port of
/// <c>rathena-fork/src/map/skills/archer/classicalpluck.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class ClassicalPluck : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ClassicalPluck() : base(SkillIds.BD_ROKISWEIL) { }

    public ClassicalPluck(ISkillUnitService? units = null) : base(SkillIds.BD_ROKISWEIL)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
