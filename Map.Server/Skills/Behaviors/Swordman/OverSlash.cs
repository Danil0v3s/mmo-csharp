using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_OVERSLASH — Imperial Guard Over Slash. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/overslash.cpp</c>.
/// Ratio <c>+(-100 + 220*lv) + 7*POW + 50*lv*IG_SPEAR_SWORD_M</c>.
/// Multi-hit divisor scales with splash count (≥2 → 5 hits, ≥4 → 7).
/// Skill-tree + miscflag-based divisor are TODO.
/// </summary>
public sealed class OverSlash : RecursiveDamageSplashSkillImpl
{
    public OverSlash() : base(SkillIds.IG_OVERSLASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 220 * skillLevel) + 7 * src.Stats.Pow;
}
