using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_ABSOLUTE_ZEPHYR — Homunculus Absolute Zephyr. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_absolutezephyr.cpp</c>.
/// Ratio <c>+(-100 + 1000 + 450*lv*BaseLv/100) + INT</c>.
/// </summary>
public sealed class AbsoluteZephyr : RecursiveDamageSplashSkillImpl
{
    public AbsoluteZephyr() : base(SkillIds.MH_ABSOLUTE_ZEPHYR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1000 + 450 * skillLevel * src.Level / 100) + src.Stats.IntStat;
}
