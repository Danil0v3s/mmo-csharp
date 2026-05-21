using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_GUST — Elemental Gust. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/gust.cpp</c>.
/// Toggles SC_GUST on target + SC_GUST_OPTION on the elemental.
/// </summary>
public sealed class Gust : SkillImpl
{
    public Gust() : base(SkillIds.EL_GUST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Gust) != null
            || ctx.Sc?.Get(src, StatusType.GustOption) != null)
        {
            ctx.Sc.End(target, StatusType.Gust);
            ctx.Sc.End(src, StatusType.GustOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.GustOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.Gust, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
