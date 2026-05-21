using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MS_BOWLINGBASH — Mercenary Bowling Bash. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_bowlingbash.cpp</c>.
/// Ratio <c>+40*lv</c>. Gutter-chain knockback logic is TODO.
/// </summary>
public sealed class MercenaryBowlingBash : WeaponSkillImpl
{
    public MercenaryBowlingBash() : base(SkillIds.MS_BOWLINGBASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 40 * skillLevel;
}
