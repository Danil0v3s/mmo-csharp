using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_BLAZING_AND_FURIOUS — Homunculus Blazing and Furious. Manual
/// port of <c>rathena-fork/src/map/skills/homunculus/homunculus_blazingandfurious.cpp</c>.
/// Ratio <c>+(-100 + 80*lv*BaseLv/100) + STR</c>. Hit count = spirit
/// ball stacks (TODO).
/// </summary>
public sealed class BlazingAndFurious : RecursiveDamageSplashSkillImpl
{
    public BlazingAndFurious() : base(SkillIds.MH_BLAZING_AND_FURIOUS) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 80 * skillLevel * src.Level / 100) + src.Stats.Str;
}
