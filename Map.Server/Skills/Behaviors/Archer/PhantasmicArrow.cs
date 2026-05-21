using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_PHANTASMIC — Hunter Phantasmic Arrow. Manual port of
/// <c>rathena-fork/src/map/skills/archer/phantasmicarrow.cpp</c>.
/// Renewal ratio: <c>+400</c>.
/// </summary>
public sealed class PhantasmicArrow : WeaponSkillImpl
{
    public PhantasmicArrow() : base(SkillIds.HT_PHANTASMIC) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 400;
}
