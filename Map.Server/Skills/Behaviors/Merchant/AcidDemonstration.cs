using Map.Server.Entities;
using Map.Server.Inventory;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// CR_ACIDDEMONSTRATION — Creator Acid Demonstration. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/aciddemonstration.cpp</c>.
/// Renewal ratio: <c>+(-100 + 200*lv) + INT + tgt_VIT</c>; halved
/// against players. On-hit: <c>skill_break_equip(EQP_WEAPON|EQP_ARMOR,
/// rate = 100*lv, BCT_ENEMY)</c> — routed through
/// <see cref="ISkillSideEffectService.BreakEquip"/>.
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

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_break_equip(src, target, EQP_WEAPON|EQP_ARMOR, 100*lv, BCT_ENEMY).
        ctx.SideEffect?.BreakEquip(src, target, (int)(EquipBits.HandR | EquipBits.Armor), rate: 100 * skillLevel);
    }
}
