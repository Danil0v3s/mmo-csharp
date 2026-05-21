using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_BABY — Baby cry-for-help. Manual port of
/// <c>rathena-fork/src/map/skills/other/baby.cpp</c>.
/// Stuns the caster (baby) and applies the adoption SC to father /
/// mother. Adoption family lookup is TODO; we land the animation.
/// </summary>
public sealed class Baby : SkillImpl
{
    public Baby() : base(SkillIds.WE_BABY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: pc_get_father / pc_get_mother lookups + same-party + same-screen gates,
        // then SC_BABY on both parents and SC_STUN on the baby.
        ctx.Sc?.Start(src, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
