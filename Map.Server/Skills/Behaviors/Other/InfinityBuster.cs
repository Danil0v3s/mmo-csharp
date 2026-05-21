using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ABR_INFINITY_BUSTER — ABR Infinity Buster. Manual port of
/// <c>rathena-fork/src/map/skills/other/infinitybuster.cpp</c>.
/// Ratio <c>+(-100 + 50000)</c>.
/// </summary>
public sealed class InfinityBuster : WeaponSkillImpl
{
    public InfinityBuster() : base(SkillIds.ABR_INFINITY_BUSTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 50000);
}
