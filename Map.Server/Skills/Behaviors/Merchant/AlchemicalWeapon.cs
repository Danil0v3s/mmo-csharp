using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_CP_WEAPON — Alchemist Chemical Protection: Weapon. Manual port
/// of <c>rathena-fork/src/map/skills/merchant/alchemicalweapon.cpp</c>.
/// Requires the recipient to be a player wearing a right-hand weapon
/// (rAthena <c>pc_checkequip(EQP_WEAPON)</c>). Applies SC_CP_WEAPON.
/// </summary>
public sealed class AlchemicalWeapon : SkillImpl
{
    public AlchemicalWeapon() : base(SkillIds.AM_CP_WEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity pc)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        var session = ctx.Sessions?.TryGet(pc);
        if (session != null && ctx.Equip != null && ctx.Equip.CheckEquip(session, EquipBits.HandR) < 0)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Sc?.Start(target, StatusType.CpWeapon, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
