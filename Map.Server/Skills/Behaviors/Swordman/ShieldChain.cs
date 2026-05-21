using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// PA_SHIELDCHAIN — Paladin Shield Chain. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/shieldchain.cpp</c>.
/// Renewal: <c>+(-100 + 300 + 200*lv)</c>; shield-weight + refine bonus
/// and SC_SHIELD_POWER +50 % multiplier are TODO (need equip access).
/// </summary>
public sealed class ShieldChain : WeaponSkillImpl
{
    public ShieldChain() : base(SkillIds.PA_SHIELDCHAIN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 + 200 * skillLevel);
}
