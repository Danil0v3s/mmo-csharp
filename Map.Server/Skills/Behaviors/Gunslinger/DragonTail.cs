using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_D_TAIL — Rebellion Dragon Tail. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/dragontail.cpp</c>.
/// Ratio <c>+(-100 + 500 + 200*lv)</c>; doubles in alternate-damage path.
/// </summary>
public sealed class DragonTail : RecursiveDamageSplashSkillImpl
{
    public DragonTail() : base(SkillIds.RL_D_TAIL) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 500 + 200 * skillLevel);
}
