using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_CP_SHIELD — Chemical Protection Shield. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/synthesizedshield.cpp</c>.
/// Requires the recipient to be a player wearing a shield. Applies
/// SC_CP_SHIELD; the shield-equip check still has to be wired through
/// the equip service.
/// </summary>
public sealed class SynthesizedShield : SkillImpl
{
    public SynthesizedShield() : base(SkillIds.AM_CP_SHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        // TODO: gate on pc_checkequip(target, EQP_SHIELD) < 0 → SkillFailCause.Level.
        ctx.Sc?.Start(target, StatusType.CpShield, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
