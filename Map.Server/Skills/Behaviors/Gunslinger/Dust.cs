using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_DUST — Gunslinger Dust. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/dust.cpp</c>. Ratio <c>+50*lv</c>.
/// </summary>
public sealed class Dust : WeaponSkillImpl
{
    public Dust() : base(SkillIds.GS_DUST) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 * skillLevel;
}
