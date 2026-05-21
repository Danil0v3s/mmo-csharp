using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_METALIC_FURY — Trouvere Metallic Fury. Manual port of
/// <c>rathena-fork/src/map/skills/archer/metallicfury.cpp</c>.
/// Ratio <c>+(-100 + 3850*lv)</c>; SC_SOUNDBLEND adds
/// <c>800*lv + 2*TR_STAGE_MANNER*SPL</c> (passive + target SC TODO).
/// </summary>
public sealed class MetallicFury : RecursiveDamageSplashSkillImpl
{
    public MetallicFury() : base(SkillIds.TR_METALIC_FURY) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 3850 * skillLevel);
    }
}
