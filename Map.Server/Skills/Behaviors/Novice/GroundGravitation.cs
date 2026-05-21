using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_GROUND_GRAVITATION — Hyper Novice Ground Gravitation. Manual
/// port of <c>rathena-fork/src/map/skills/novice/groundgravitation.cpp</c>.
/// Field-tick variant ratio <c>+(-100 + 800 + 700*lv) + 2*SPL</c>;
/// initial drop variant is <c>+(-100 + 3000 + 1500*lv) + 5*SPL</c>.
/// We use the field-tick formula by default. HN_SELFSTUDY_SOCERY
/// amp + SC_RULEBREAK boost are TODO.
/// </summary>
public sealed class GroundGravitation : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public GroundGravitation() : base(SkillIds.HN_GROUND_GRAVITATION) { }

    public GroundGravitation(ISkillUnitService? units = null) : base(SkillIds.HN_GROUND_GRAVITATION)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 800 + 700 * skillLevel) + 2 * src.Stats.Spl;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
