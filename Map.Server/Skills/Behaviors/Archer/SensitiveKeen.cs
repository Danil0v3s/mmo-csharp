using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_SENSITIVEKEEN — Ranger Sensitive Keen. Manual port of
/// <c>rathena-fork/src/map/skills/archer/sensitivekeen.cpp</c>.
/// Reveals hidden enemies + traps in splash. Ratio: <c>+50*lv</c>.
/// Hidden-target dispatch + trap-iteration TODOs.
/// </summary>
public sealed class SensitiveKeen : WeaponSkillImpl
{
    public SensitiveKeen() : base(SkillIds.RA_SENSITIVEKEEN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 * skillLevel;
}
