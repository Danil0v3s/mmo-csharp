using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_MAGNIFICAT — Mercenary Magnificat. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_magnificat.cpp</c>.
/// Applies SC_MAGNIFICAT to the target; splashes to master + party
/// when invoked from a mercenary linked to a party. Party splash is TODO.
/// </summary>
public sealed class MercenaryMagnificat : SkillImpl
{
    public MercenaryMagnificat() : base(SkillIds.MER_MAGNIFICAT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Magnificat, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
