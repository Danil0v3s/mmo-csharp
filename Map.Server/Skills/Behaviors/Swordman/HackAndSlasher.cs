using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_HACKANDSLASHER — Dragon Knight Hack and Slasher. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/hackandslasher.cpp</c>.
/// Ratio <c>+(-100 + 350 + 820*lv) + 7*POW</c>.
/// </summary>
public sealed class HackAndSlasher : RecursiveDamageSplashSkillImpl
{
    public HackAndSlasher() : base(SkillIds.DK_HACKANDSLASHER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 350 + 820 * skillLevel) + 7 * src.Stats.Pow;
}
