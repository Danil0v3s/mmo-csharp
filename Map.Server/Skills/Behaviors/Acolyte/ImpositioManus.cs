using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_IMPOSITIO — Priest Impositio Manus. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/impositiomanus.cpp</c>.
///
/// <para>Single / party-wide +ATK buff (<c>5 * skill_lv</c>). The
/// renewal branch uses the same party-dispatch envelope as
/// Magnificat / Suffragium; pre-renewal uses
/// <c>StatusSkillImpl</c>'s generic apply.</para>
///
/// <para>Duration 120 000 ms fixed. Val1 = skill level (consumed by
/// the SC handler when computing the +ATK delta).</para>
/// </summary>
public sealed class ImpositioManus : SkillImpl
{
    public ImpositioManus() : base(SkillIds.PR_IMPOSITIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Impositio, val1: skillLevel, 0, 0, 0, 120_000, src);
        // TODO: party_foreachsamemap (see Angelus).
    }
}
