using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// CR_FULLPROTECTION — Crusader Full Protection. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/fullprotection.cpp</c>.
/// Applies SC_CP_WEAPON/SHIELD/ARMOR/HELM to a player target — but
/// only for slots the target actually has gear in (rAthena walks
/// {EQP_WEAPON, EQP_SHIELD, EQP_ARMOR, EQP_HEAD_TOP} and skips empty
/// slots). Fails if every slot is empty.
/// </summary>
public sealed class FullProtection : SkillImpl
{
    public FullProtection() : base(SkillIds.CR_FULLPROTECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity pc)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        const int duration = 60_000;
        var session = ctx.Sessions?.TryGet(pc);
        // Equip-aware path — only applies a CP_* SC when the target has
        // gear in the matching slot. Matches rAthena's per-slot loop.
        // When no session/equip service is wired (unit tests), fall back
        // to the all-four-slots application so the cast still resolves.
        var applied = 0;
        if (session != null && ctx.Equip != null)
        {
            if (ctx.Equip.CheckEquip(session, EquipBits.HandR) >= 0)
            { ctx.Sc?.Start(target, StatusType.CpWeapon, val1: skillLevel, 0, 0, 0, durationMs: duration, src); applied++; }
            if (ctx.Equip.CheckEquip(session, EquipBits.HandL) >= 0)
            { ctx.Sc?.Start(target, StatusType.CpShield, val1: skillLevel, 0, 0, 0, durationMs: duration, src); applied++; }
            if (ctx.Equip.CheckEquip(session, EquipBits.Armor) >= 0)
            { ctx.Sc?.Start(target, StatusType.CpArmor, val1: skillLevel, 0, 0, 0, durationMs: duration, src); applied++; }
            if (ctx.Equip.CheckEquip(session, EquipBits.HeadTop) >= 0)
            { ctx.Sc?.Start(target, StatusType.CpHelm, val1: skillLevel, 0, 0, 0, durationMs: duration, src); applied++; }
            if (applied == 0)
            {
                if (src is PlayerEntity sd)
                    ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
                return;
            }
        }
        else
        {
            ctx.Sc?.Start(target, StatusType.CpWeapon, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
            ctx.Sc?.Start(target, StatusType.CpShield, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
            ctx.Sc?.Start(target, StatusType.CpArmor, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
            ctx.Sc?.Start(target, StatusType.CpHelm, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
