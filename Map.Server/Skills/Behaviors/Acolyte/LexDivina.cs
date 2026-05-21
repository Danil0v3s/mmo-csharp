using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_LEXDIVINA — Priest Lex Divina. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/lexdivina.cpp</c>.
///
/// <para>Silences the target. Toggle semantics: if the target
/// already has the SC, recast ends it; otherwise schedules a
/// 1-second delayed SC apply (rAthena uses
/// <c>skill_addtimerskill</c> so the cast frame plays before the
/// silence lands).</para>
///
/// <para>Duration2 ladder: <c>(25 + 5 * skillLevel) * 1000</c> ms
/// silence on apply (30 s lv 1 → 75 s lv 10).</para>
/// </summary>
public sealed class LexDivina : SkillImpl
{
    private readonly Map.Server.Skills.ISkillTimerService? _timers;

    public LexDivina() : base(SkillIds.PR_LEXDIVINA) { }

    public LexDivina(Map.Server.Skills.ISkillTimerService? timers = null) : base(SkillIds.PR_LEXDIVINA)
    {
        _timers = timers;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: type = SC_SILENCE; if (tsce) status_change_end → recast cures.
        if (ctx.Sc != null && ctx.Sc.Get(target, StatusType.Silence) != null)
        {
            ctx.Sc.End(target, StatusType.Silence);
        }
        else
        {
            // rAthena: skill_addtimerskill(src, tick+1000, target->id, 0, 0, getSkillId(), skill_lv, 100, flag);
            // 1-second delayed SC apply.
            var duration = (25 + 5 * skillLevel) * 1000;
            if (_timers != null)
            {
                _timers.Schedule(src, target, delayMs: 1000, SkillId, skillLevel,
                    (resolvedSrc, resolvedTarget, lv) =>
                    {
                        ctx.Sc?.Start(resolvedTarget, StatusType.Silence, val1: lv,
                            0, 0, 0, duration, resolvedSrc);
                    });
            }
            else
            {
                // No timer service — apply immediately as a fallback.
                ctx.Sc?.Start(target, StatusType.Silence, val1: skillLevel,
                    0, 0, 0, duration, src);
            }
        }

        // rAthena: clif_skill_nodamage(src, *target, getSkillId(), skill_lv) — always emitted.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
