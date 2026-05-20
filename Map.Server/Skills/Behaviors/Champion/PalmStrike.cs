using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Champion;

/// <summary>
/// CH_PALMSTRIKE — Champion Palm Push Strike. Mirrors
/// <c>rathena-fork/src/map/skills/champion/palmstrike.cpp</c>.
///
/// Single-target physical at (100 + 100*lv)% ATK + knockback 1
/// cell. Combo opener for the Champion finisher chain.
/// </summary>
public sealed class PalmStrike : WeaponSkillImpl
{
    public PalmStrike() : base(SkillIds.CH_PALMSTRIKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * skillLevel;
}
