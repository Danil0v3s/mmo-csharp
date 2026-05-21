using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// ML_PIERCE — Mercenary Pierce. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_pierce.cpp</c>.
/// Ratio <c>+10*lv</c>; hit chance <c>+5*lv%</c>. Hit-count scales with
/// target size — TODO.
/// </summary>
public sealed class MercenaryPierce : WeaponSkillImpl
{
    public MercenaryPierce() : base(SkillIds.ML_PIERCE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 * skillLevel;

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 5 * skillLevel / 100);
}
