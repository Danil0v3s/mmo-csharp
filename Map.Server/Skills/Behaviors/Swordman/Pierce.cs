using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_PIERCE — Knight Pierce. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/pierce.cpp</c>.
/// Ratio <c>+10*lv</c>; doubles with SC_CHARGINGPIERCE_COUNT val1 ≥ 10
/// (SC plumb TODO). Hit chance bonus <c>+5*lv %</c>. Multi-hit count
/// scales with target size — divisor not yet plumbed.
/// </summary>
public sealed class Pierce : WeaponSkillImpl
{
    public Pierce() : base(SkillIds.KN_PIERCE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 * skillLevel;

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 5 * skillLevel / 100);
}
