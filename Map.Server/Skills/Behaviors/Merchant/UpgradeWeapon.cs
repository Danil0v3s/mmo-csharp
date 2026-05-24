using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// WS_WEAPONREFINE — Whitesmith Upgrade Weapon. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/upgradeweapon.cpp</c>.
/// Opens the client-side refine list. UI plumbing is TODO.
/// </summary>
public sealed class UpgradeWeapon : SkillImpl
{
    public UpgradeWeapon() : base(SkillIds.WS_WEAPONREFINE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: clif_item_refine_list emits ZC_NOTIFY_WEAPONITEMLIST (refine UI)
        // and the refine catalog hooks aren't ported yet.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
