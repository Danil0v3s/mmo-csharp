using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_HEILIGE_STANGE — Homunculus Holy Pole. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_holypole.cpp</c>.
/// Ratio <c>+(-100 + 1500 + 250*lv*BaseLv/150) + VIT</c>.
/// </summary>
public sealed class HolyPole : RecursiveDamageSplashSkillImpl
{
    public HolyPole() : base(SkillIds.MH_HEILIGE_STANGE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1500 + 250 * skillLevel * src.Level / 150) + src.Stats.Vit;
}
