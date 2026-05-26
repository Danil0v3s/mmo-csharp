using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_FLAMELAUNCHER — Sage Endow Blaze. Manual port of
/// <c>rathena-fork/src/map/skills/mage/endowblaze.cpp</c>.
///
/// <para>Endows the target's weapon with the Fire element (SC_FIREWEAPON).
/// rAthena's Renewal branch lands at 100 % rate; the legacy branch
/// rolls <c>60 + 10*lv %</c> and unequips on failure. We follow the
/// Renewal path here. Fails if the target is unarmed
/// (<see cref="PlayerEntity.WeaponType"/> == W_FIST).</para>
/// </summary>
public sealed class EndowBlaze : SkillImpl
{
    /// <summary>rAthena <c>W_FIST</c> = 0 (pc.hpp). Bare-hand target rejects the endow.</summary>
    private const int W_FIST = 0;

    public EndowBlaze() : base(SkillIds.SA_FLAMELAUNCHER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: fail on unarmed (W_FIST) target.
        if (target is PlayerEntity pc && pc.WeaponType == W_FIST)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.Skill);
            return;
        }
        ctx.Sc?.Start(target, StatusType.Fireweapon, val1: skillLevel, 0, 0, 0, durationMs: 60_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
