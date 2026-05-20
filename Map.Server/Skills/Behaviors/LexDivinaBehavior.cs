using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// PR_LEXDIVINA (id 76) — Priest Lex Divina. rAthena
/// <c>skill.cpp:case PR_LEXDIVINA</c> applies
/// <see cref="StatusType.Silence"/> on the target. Renewal duration:
/// <c>30 * lv</c> seconds. Recast on an already-silenced target ends
/// the SC instead (rAthena status_change_end shortcut).
/// </summary>
public sealed class LexDivinaBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.PR_LEXDIVINA;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // Re-cast cures rather than refreshes (the "silence cleanse" use).
        if (ctx.Sc.Get(target, StatusType.Silence) != null)
        {
            ctx.Sc.End(target, StatusType.Silence);
            return true;
        }

        var durationMs = 30_000 * skillLevel;
        ctx.Sc.Start(target, StatusType.Silence, val1: skillLevel, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
