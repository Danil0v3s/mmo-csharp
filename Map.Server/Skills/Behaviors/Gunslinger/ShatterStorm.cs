using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_S_STORM — Rebellion Shatter Storm. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/shatterstorm.cpp</c>.
/// Ratio <c>+(-100 + 1700 + 200*lv)</c>. Headgear break (skill_break_equip)
/// is TODO.
/// </summary>
public sealed class ShatterStorm : RecursiveDamageSplashSkillImpl
{
    public ShatterStorm() : base(SkillIds.RL_S_STORM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1700 + 200 * skillLevel);
}
