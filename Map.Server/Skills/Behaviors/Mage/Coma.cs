using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_COMA — Sage Coma. Applies SC_COMA at 100 %; target instantly
/// reduced to 1 HP (handled by the SC handler) for skill_get_time2.
/// </summary>
public sealed class Coma : SkillImpl
{
    public Coma() : base(SkillIds.SA_COMA) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        bool landed = ctx.Sc?.Start(target, StatusType.Coma,
            val1: skillLevel, 0, 0, 0, durationMs: 30_000, src) != null;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, landed);
    }
}
