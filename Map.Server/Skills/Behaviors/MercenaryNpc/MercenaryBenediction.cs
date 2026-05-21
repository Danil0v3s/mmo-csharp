using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_BENEDICTION — Mercenary Benediction. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_benediction.cpp</c>.
/// Cleanses SC_CURSE and SC_BLIND from the target.
/// </summary>
public sealed class MercenaryBenediction : SkillImpl
{
    public MercenaryBenediction() : base(SkillIds.MER_BENEDICTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Curse);
        ctx.Sc?.End(target, StatusType.Blind);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
