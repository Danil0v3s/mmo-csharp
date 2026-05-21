using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_REPAIRWEAPON — Blacksmith Repair Weapon. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/weaponrepair.cpp</c>.
/// Opens the repair list for the target. UI plumbing TODO.
/// </summary>
public sealed class WeaponRepair : SkillImpl
{
    public WeaponRepair() : base(SkillIds.BS_REPAIRWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity || target is not PlayerEntity) return;
        // TODO: clif_item_repair_list(sd, dstsd, skill_lv).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
