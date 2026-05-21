using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_INVINCIBLEOFF2 — Mercenary Mind Blaster. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_mindblaster.cpp</c>.
/// Ends SC_INVINCIBLE on the target.
/// </summary>
public sealed class MercenaryMindBlaster : SkillImpl
{
    public MercenaryMindBlaster() : base(SkillIds.MER_INVINCIBLEOFF2) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.End(target, StatusType.Invincible);
    }
}
