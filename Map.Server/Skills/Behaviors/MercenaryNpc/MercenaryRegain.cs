using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_REGAIN — Mercenary Regain. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_regain.cpp</c>.
/// Cleanses SC_SLEEP, SC_STUN.
/// </summary>
public sealed class MercenaryRegain : SkillImpl
{
    public MercenaryRegain() : base(SkillIds.MER_REGAIN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Sleep);
        ctx.Sc?.End(target, StatusType.Stun);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
