using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// CR_ACIDDEMONSTRATION — Creator Acid Demonstration. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/aciddemonstration.cpp</c>.
/// Renewal ratio: <c>+(-100 + 200*lv) + INT + tgt_VIT</c>; halved
/// against players. Breaks weapon+armor on hit (TODO).
/// </summary>
public sealed class AcidDemonstration : WeaponSkillImpl
{
    public AcidDemonstration() : base(SkillIds.CR_ACIDDEMONSTRATION) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 200 * skillLevel) + src.Stats.IntStat + target.Stats.Vit;
        if (target is PlayerEntity) ratio /= 2;
        return ratio;
    }
}
