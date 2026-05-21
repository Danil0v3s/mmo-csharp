using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_MAGMA_FLOW — Homunculus Magma Flow. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_magmaflow.cpp</c>.
/// Ratio <c>+(-100 + (100*lv + 3*BaseLv) * BaseLv / 120)</c>. 3*lv% to
/// fire follow-up.
/// </summary>
public sealed class MagmaFlow : RecursiveDamageSplashSkillImpl
{
    public MagmaFlow() : base(SkillIds.MH_MAGMA_FLOW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + (100 * skillLevel + 3 * src.Level) * src.Level / 120);
}
