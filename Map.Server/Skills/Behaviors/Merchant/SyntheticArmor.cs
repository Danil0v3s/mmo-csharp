using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_CP_ARMOR — Chemical Protection Armor. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/syntheticarmor.cpp</c>.
/// Requires the recipient to be a player wearing armor. Applies
/// SC_CP_ARMOR; equip check is TODO.
/// </summary>
public sealed class SyntheticArmor : SkillImpl
{
    public SyntheticArmor() : base(SkillIds.AM_CP_ARMOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        // TODO: gate on pc_checkequip(target, EQP_ARMOR) < 0 → SkillFailCause.Level.
        ctx.Sc?.Start(target, StatusType.CpArmor, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
