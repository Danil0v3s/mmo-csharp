using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MS_BASH — Mercenary Bash. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_bash.cpp</c>.
/// Ratio <c>+30*lv</c>; hit chance bonus <c>+5*lv%</c>.
/// </summary>
public sealed class MercenaryBash : WeaponSkillImpl
{
    public MercenaryBash() : base(SkillIds.MS_BASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 30 * skillLevel;

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 5 * skillLevel / 100);
}
