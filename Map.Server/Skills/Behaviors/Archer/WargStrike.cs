using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGSTRIKE — Ranger Warg Strike. Manual port of
/// <c>rathena-fork/src/map/skills/archer/wargstrike.cpp</c>.
/// Ratio <c>+(-100 + 200*lv)</c>. Mounted dash-then-hit is TODO.
/// </summary>
public sealed class WargStrike : WeaponSkillImpl
{
    public WargStrike() : base(SkillIds.RA_WUGSTRIKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel);
}
