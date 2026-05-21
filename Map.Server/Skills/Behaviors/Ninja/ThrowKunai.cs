using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_KUNAI — Throw Kunai. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/throwkunai.cpp</c>.
/// Renewal: <c>+(-100 + 100*lv)</c> ratio; pre-renewal flat.
/// </summary>
public sealed class ThrowKunai : WeaponSkillImpl
{
    public ThrowKunai() : base(SkillIds.NJ_KUNAI) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 100 * skillLevel);
}
