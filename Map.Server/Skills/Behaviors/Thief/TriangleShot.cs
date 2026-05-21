using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_TRIANGLESHOT — Triangle Shot. Manual port of
/// <c>rathena-fork/src/map/skills/thief/triangleshot.cpp</c>.
/// Ratio <c>+(-100 + 230*lv) + 3*Agi</c>.
/// </summary>
public sealed class TriangleShot : WeaponSkillImpl
{
    public TriangleShot() : base(SkillIds.SC_TRIANGLESHOT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 230 * skillLevel) + 3 * src.Stats.Agi;
}
