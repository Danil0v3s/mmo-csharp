using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_MENTALCURE — Mercenary Mental Cure. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_mentalcure.cpp</c>.
/// Cleanses SC_CONFUSION.
/// </summary>
public sealed class MercenaryMentalCure : SkillImpl
{
    public MercenaryMentalCure() : base(SkillIds.MER_MENTALCURE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Confusion);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
