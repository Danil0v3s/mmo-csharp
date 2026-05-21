using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_TENDER — Mercenary Tender. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_tender.cpp</c>.
/// Cleanses SC_FREEZE and SC_STONE.
/// </summary>
public sealed class MercenaryTender : SkillImpl
{
    public MercenaryTender() : base(SkillIds.MER_TENDER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Freeze);
        ctx.Sc?.End(target, StatusType.Stone);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
