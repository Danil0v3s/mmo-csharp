using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_CP_SHIELD — Chemical Protection Shield. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/synthesizedshield.cpp</c>.
/// Requires the recipient to be a player wearing a shield. Applies
/// SC_CP_SHIELD; equip slot is gated via <see cref="IEquipService.CheckEquip"/>.
/// </summary>
public sealed class SynthesizedShield : SkillImpl
{
    public SynthesizedShield() : base(SkillIds.AM_CP_SHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity pc)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        // rAthena pc_checkequip(target, EQP_SHIELD) < 0 → fail with Level.
        var session = ctx.Sessions?.TryGet(pc);
        if (session != null && ctx.Equip != null && ctx.Equip.CheckEquip(session, EquipBits.HandL) < 0)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Sc?.Start(target, StatusType.CpShield, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
