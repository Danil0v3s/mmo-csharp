using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_STYLE_CHANGE — Homunculus Style Change. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_stylechange.cpp</c>.
/// Toggles SC_STYLE_CHANGE between Fighting and Grappling stances.
/// Stance toggle / infinite-duration management is TODO; we apply a
/// 30 min buff as the placeholder.
/// </summary>
public sealed class StyleChange : SkillImpl
{
    public StyleChange() : base(SkillIds.MH_STYLE_CHANGE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(src, StatusType.StyleChange) != null)
            ctx.Sc.End(src, StatusType.StyleChange);
        else
            ctx.Sc?.Start(src, StatusType.StyleChange, val1: skillLevel, 0, 0, 0, durationMs: 30 * 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
