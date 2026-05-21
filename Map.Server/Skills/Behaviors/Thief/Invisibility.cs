using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_INVISIBILITY — Invisibility. Manual port of
/// <c>rathena-fork/src/map/skills/thief/invisibility.cpp</c>.
/// Toggles SC_INVISIBILITY on the target.
/// </summary>
public sealed class Invisibility : SkillImpl
{
    public Invisibility() : base(SkillIds.SC_INVISIBILITY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Invisibility) != null)
        {
            ctx.Sc.End(target, StatusType.Invisibility);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            return;
        }
        ctx.Sc?.Start(target, StatusType.Invisibility, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
