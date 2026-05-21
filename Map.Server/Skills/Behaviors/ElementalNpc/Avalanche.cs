using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_AVALANCHE — Elemental Avalanche. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/avalanche.cpp</c>.
/// Ratio <c>+(-100 + 450)</c>, scaled by (1 + masterLv/100).
/// </summary>
public sealed class Avalanche : RecursiveDamageSplashSkillImpl
{
    public Avalanche() : base(SkillIds.EM_EL_AVALANCHE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 450);
        ratio += ratio * src.Level / 100;
        return ratio;
    }
}
