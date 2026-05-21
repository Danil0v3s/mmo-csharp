using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_MUSICALSTRIKE — Bard Melody Strike (Musical Strike). Manual
/// port of <c>rathena-fork/src/map/skills/archer/melodystrike.cpp</c>.
/// Renewal ratio: <c>+(10 + 40*lv)</c>.
/// </summary>
public sealed class MelodyStrike : WeaponSkillImpl
{
    public MelodyStrike() : base(SkillIds.BA_MUSICALSTRIKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 + 40 * skillLevel;
}
