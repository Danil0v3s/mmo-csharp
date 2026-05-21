using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_KYRIE — Mercenary Kyrie Eleison. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_kyrieeleison.cpp</c>.
/// Applies SC_KYRIE.
/// </summary>
public sealed class MercenaryKyrieEleison : SkillImpl
{
    public MercenaryKyrieEleison() : base(SkillIds.MER_KYRIE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Kyrie, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
