using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_RAPIDSHOWER — Gunslinger Rapid Shower. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/rapidshower.cpp</c>.
/// Ratio <c>+(400 + 50*lv)</c>.
/// </summary>
public sealed class RapidShower : WeaponSkillImpl
{
    public RapidShower() : base(SkillIds.GS_RAPIDSHOWER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 400 + 50 * skillLevel;
}
