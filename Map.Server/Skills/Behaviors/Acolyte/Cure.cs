using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_CURE — Acolyte Cure. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/cure.cpp</c>.
///
/// <para>Removes Silence, Blind, Confusion and Bitescar from the
/// target. Status-immune mobs (MD_STATUSIMMUNE) cause the cast to
/// fail-broadcast with no SC removal.</para>
/// </summary>
public sealed class Cure : SkillImpl
{
    public Cure() : base(SkillIds.AL_CURE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: if (status_isimmune(bl)) { clif_skill_nodamage(..., false); return; }
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, success: false);
            return;
        }

        // rAthena: end SC_SILENCE / SC_BLIND / SC_CONFUSION / SC_BITESCAR.
        ctx.Sc?.End(target, StatusType.Silence);
        ctx.Sc?.End(target, StatusType.Blind);
        ctx.Sc?.End(target, StatusType.Confusion);
        ctx.Sc?.End(target, StatusType.Bitescar);

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
