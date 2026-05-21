using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HFLI_MOON — Filir Moonlight. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_moonlight.cpp</c>.
/// Ratio <c>+(10 + 110*lv)</c>.
/// </summary>
public sealed class Moonlight : WeaponSkillImpl
{
    public Moonlight() : base(SkillIds.HFLI_MOON) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 + 110 * skillLevel;
}
