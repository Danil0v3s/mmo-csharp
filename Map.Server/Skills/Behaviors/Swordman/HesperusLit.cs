using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_HESPERUSLIT — Royal Guard Hesperus:Lit. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/hesperuslit.cpp</c>.
/// Ratio <c>+(-100 + 300*lv) + VIT/6</c>; <c>+(-100 + 450*lv)</c> with
/// SC_INSPIRATION. SC plumbing into ratio + Pinpoint Attack chain
/// proc are TODO.
/// </summary>
public sealed class HesperusLit : WeaponSkillImpl
{
    public HesperusLit() : base(SkillIds.LG_HESPERUSLIT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 * skillLevel) + src.Stats.Vit / 6;
    // TODO: when SC_INSPIRATION is active, ratio becomes +(-100 + 450*lv).
}
