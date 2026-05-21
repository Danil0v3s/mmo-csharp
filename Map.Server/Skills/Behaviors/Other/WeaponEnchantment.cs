using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ITEM_ENCHANTARMS — Weapon enchantment (item-script). Manual port of
/// <c>rathena-fork/src/map/skills/other/weaponenchantment.cpp</c>.
/// Applies SC_ENCHANTARMS with the element from skill_get_ele. Element
/// lookup is TODO; we land the animation.
/// </summary>
public sealed class WeaponEnchantment : SkillImpl
{
    public WeaponEnchantment() : base(SkillIds.ITEM_ENCHANTARMS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
