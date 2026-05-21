using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// ML_SPIRALPIERCE — Mercenary Spiral Pierce. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_spiralpierce.cpp</c>.
/// Renewal ratio <c>+50 + 50*lv</c>.
/// </summary>
public sealed class MercenarySpiralPierce : WeaponSkillImpl
{
    public MercenarySpiralPierce() : base(SkillIds.ML_SPIRALPIERCE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 + 50 * skillLevel;
}
