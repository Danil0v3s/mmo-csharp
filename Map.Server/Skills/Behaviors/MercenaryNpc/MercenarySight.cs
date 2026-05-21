using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_SIGHT — Mercenary Sight. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_sight.cpp</c>.
/// Applies SC_SIGHT with val2 = skill id.
/// </summary>
public sealed class MercenarySight : SkillImpl
{
    public MercenarySight() : base(SkillIds.MER_SIGHT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Sight, val1: skillLevel, val2: SkillId, 0, 0, durationMs: 10_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
