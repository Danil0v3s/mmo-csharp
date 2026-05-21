using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_GLANZEN_SPIES — Homunculus Glanzen Spies. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_glanzenspies.cpp</c>.
/// Ratio <c>+(-100 + 300 + 450*lv*BaseLv/100) + VIT</c>.
/// </summary>
public sealed class GlanzenSpies : SkillImpl
{
    public GlanzenSpies() : base(SkillIds.MH_GLANZEN_SPIES) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 + 450 * skillLevel * src.Level / 100) + src.Stats.Vit;
}
