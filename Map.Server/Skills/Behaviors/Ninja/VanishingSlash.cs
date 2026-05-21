using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_KASUMIKIRI — Vanishing Slash. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/vanishingslash.cpp</c>.
/// Renewal: <c>+20*lv</c> ratio bump. Applies SC_KASUMIKIRI to self
/// (handled via existing additional-effect chain TODO).
/// </summary>
public sealed class VanishingSlash : WeaponSkillImpl
{
    public VanishingSlash() : base(SkillIds.NJ_KASUMIKIRI) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 20 * skillLevel;
}
