using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_MADNESS_CRUSHER — Dragon Knight Madness Crusher. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/madnesscrusher.cpp</c>.
/// Ratio <c>+(-100 + 1000 + 3800*lv) + 10*POW</c>. Weapon weight ×
/// weapon level bonus and ChargingPierceCount-x2 are TODO (need
/// inventory + SC plumbing into ratio).
/// </summary>
public sealed class MadnessCrusher : RecursiveDamageSplashSkillImpl
{
    public MadnessCrusher() : base(SkillIds.DK_MADNESS_CRUSHER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1000 + 3800 * skillLevel) + 10 * src.Stats.Pow;
}
