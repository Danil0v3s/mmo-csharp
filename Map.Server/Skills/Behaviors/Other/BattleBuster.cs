using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ABR_BATTLE_BUSTER — ABR Battle Buster. Manual port of
/// <c>rathena-fork/src/map/skills/other/battlebuster.cpp</c>.
/// Ratio <c>+(-100 + 8000)</c>.
/// </summary>
public sealed class BattleBuster : WeaponSkillImpl
{
    public BattleBuster() : base(SkillIds.ABR_BATTLE_BUSTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 8000);
}
