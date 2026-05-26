using Map.Server.Entities;
using Map.Server.Inventory;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_ADVANCE_PROTECTION — Biolo Advance Protection. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/advanceprotection.cpp</c>.
/// Requires the target to be a player with at least one Shadow Gear
/// piece equipped (rAthena <c>pc_checkequip(EQP_SHADOW_GEAR)</c>).
/// On gate pass, delegates to <see cref="StatusSkillImpl"/> which
/// applies the configured SC.
/// </summary>
public sealed class AdvanceProtection : StatusSkillImpl
{
    public AdvanceProtection() : base(SkillIds.BO_ADVANCE_PROTECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity pc)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        var session = ctx.Sessions?.TryGet(pc);
        if (session != null && ctx.Equip != null && ctx.Equip.CheckEquip(session, EquipBits.ShadowGear) < 0)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        base.CastendNoDamageId(src, target, skillLevel, ctx);
    }
}
