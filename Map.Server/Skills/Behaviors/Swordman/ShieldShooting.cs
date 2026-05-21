using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_SHIELD_SHOOTING — Imperial Guard Shield Shooting. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/shieldshooting.cpp</c>.
/// Ratio <c>+(-100 + 1000 + 3500*lv) + 10*POW</c>. Shield weight /
/// refine / Shield Mastery bonuses are TODO.
/// </summary>
public sealed class ShieldShooting : RecursiveDamageSplashSkillImpl
{
    public ShieldShooting() : base(SkillIds.IG_SHIELD_SHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1000 + 3500 * skillLevel) + 10 * src.Stats.Pow;
}
