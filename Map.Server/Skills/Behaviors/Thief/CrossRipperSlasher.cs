using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_CROSSRIPPERSLASHER — Cross Ripper Slasher. Manual port of
/// <c>rathena-fork/src/map/skills/thief/crossripperslasher.cpp</c>.
/// Ratio <c>+(-100 + 80*lv + 3*Agi)</c>. Requires SC_ROLLINGCUTTER;
/// +val1*200 ratio per cutter spin. Cutter prerequisite check is TODO.
/// </summary>
public sealed class CrossRipperSlasher : WeaponSkillImpl
{
    public CrossRipperSlasher() : base(SkillIds.GC_CROSSRIPPERSLASHER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 80 * skillLevel) + src.Stats.Agi * 3;
}
