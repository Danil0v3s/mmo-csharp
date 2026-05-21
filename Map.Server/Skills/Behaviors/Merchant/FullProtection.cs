using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// CR_FULLPROTECTION — Crusader Full Protection. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/fullprotection.cpp</c>.
/// Applies SC_CP_WEAPON/SHIELD/ARMOR/HELM to a player target.
/// Equip-slot check TODO.
/// </summary>
public sealed class FullProtection : SkillImpl
{
    public FullProtection() : base(SkillIds.CR_FULLPROTECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        const int duration = 60_000;
        ctx.Sc?.Start(target, StatusType.CpWeapon, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
        ctx.Sc?.Start(target, StatusType.CpShield, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
        ctx.Sc?.Start(target, StatusType.CpArmor, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
        ctx.Sc?.Start(target, StatusType.CpHelm, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
