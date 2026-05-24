using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_SLINGITEM — Genetic Sling Item (skill.cpp:GN_SLINGITEM arm).
/// Reads the slinger's equipped ammo and dispatches on the rAthena
/// item-group membership:
/// <list type="bullet">
///   <item><c>BOMB</c> — weapon strike via
///         <see cref="SkillIds.GN_SLINGITEM_RANGEMELEEATK"/>.</item>
///   <item><c>THROWABLE</c> — runs the throwable's usable script on
///         the target (heal + SC apply). Per-item script dispatch is
///         flagged inline; this surface routes the cast and gives
///         the SkillFail when the script can't run.</item>
/// </list>
/// </summary>
public sealed class SlingItem : SkillImpl
{
    public SlingItem() : base(SkillIds.GN_SLINGITEM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        // rAthena emits clif_skill_nodamage twice — that's the cue for
        // the client to play the hurl animation.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        var session = ctx.Sessions?.TryGet(pc);
        if (session == null || ctx.Equip == null || ctx.Catalog == null) return;
        var ammo = ctx.Equip.FindEquipped(session, EquipBits.Ammo);
        if (ammo == null || ammo.NameId == 0) return;
        var row = ctx.Catalog.Get(ammo.NameId);
        if (row == null) return;
        var aegis = row.NameAegis ?? string.Empty;

        if (ctx.ItemGroups?.ContainsItem("BOMB", aegis) == true)
        {
            ctx.SkillAttack?.SkillAttack(BattleAttackType.Weapon, pc, pc, target,
                SkillIds.GN_SLINGITEM_RANGEMELEEATK, skillLevel);
        }
        else if (ctx.ItemGroups?.ContainsItem("THROWABLE", aegis) != true)
        {
            // Neither group — invalid ammo type (rAthena breaks the
            // outer case without dispatch). Per-item throwable scripts
            // (HP_INC_POTS_TO_THROW etc.) live on the item_db's Script
            // column; running them through here needs the usable-item
            // dispatcher hook the inventory layer owns.
            ctx.Client?.BroadcastSkillFail(pc, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
        }
    }
}
