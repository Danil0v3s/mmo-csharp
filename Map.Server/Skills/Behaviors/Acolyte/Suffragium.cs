using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_SUFFRAGIUM — Priest Suffragium. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/suffragium.cpp</c>.
///
/// <para>Single-target / party-wide cast-time reduction. The fork's
/// renewal branch follows the standard party dispatch (same as
/// Magnificat); the pre-renewal branch uses <c>StatusSkillImpl</c>'s
/// generic SC apply. We mirror the renewal path.</para>
///
/// <para>Duration: 60 000 ms fixed; Val1 carries the cast-reduction
/// percent (consumed by <c>SkillCastTimingService.CastFixSc</c>
/// when the target casts their next spell — 15 / 30 / 45 % at
/// levels 1 / 2 / 3).</para>
/// </summary>
public sealed class Suffragium : SkillImpl
{
    public Suffragium() : base(SkillIds.PR_SUFFRAGIUM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Suffragium, val1: skillLevel, 0, 0, 0, 60_000, src);
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Suffragium, val1: skillLevel, 0, 0, 0, 60_000, src);
            }, includeSelf: false);
        }
    }
}
