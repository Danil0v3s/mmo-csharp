using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_GLORIA — Priest Gloria. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/gloria.cpp</c>.
///
/// <para>Party-wide +LUK buff (+30 LUK per skill_db). Same dispatch
/// pattern as Magnificat. Duration:
/// <c>(5 + 5 * skillLevel) * 1000</c> ms (10 s lv 1 → 55 s lv 10).</para>
/// </summary>
public sealed class Gloria : SkillImpl
{
    public Gloria() : base(SkillIds.PR_GLORIA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        var duration = (5 + 5 * skillLevel) * 1000;
        ctx.Sc?.Start(target, StatusType.Gloria, val1: skillLevel, 0, 0, 0, duration, src);
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Gloria, val1: skillLevel, 0, 0, 0, duration, src);
            }, includeSelf: false);
        }
    }
}
