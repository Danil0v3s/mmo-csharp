using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Paladin;

/// <summary>
/// PA_SHIELDCHAIN — Paladin Shield Chain. Mirrors
/// <c>rathena-fork/src/map/skills/paladin/shieldchain.cpp</c>.
///
/// Hits up to 4 enemies in succession; each at (30 + 30*lv)% ATK
/// scaled by shield weight (skipped in this first port — weight
/// table lands later). Requires shield equipped (gated upstream).
/// </summary>
public sealed class ShieldChain : WeaponSkillImpl
{
    public ShieldChain() : base(SkillIds.PA_SHIELDCHAIN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 30 * skillLevel - 70; // 30 + 30*lv overall (base 100 starts here)
}
