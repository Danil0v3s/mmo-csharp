using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_SHIELDBOOMERANG — Crusader Shield Boomerang. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/shieldboomerang.cpp</c>.
/// Renewal: <c>+(-100 + 80*lv)</c>.
/// </summary>
public sealed class ShieldBoomerang : WeaponSkillImpl
{
    public ShieldBoomerang() : base(SkillIds.CR_SHIELDBOOMERANG) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 80 * skillLevel);
}
