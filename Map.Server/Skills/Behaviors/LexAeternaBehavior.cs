using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// PR_LEXAETERNA (id 78) — Priest Lex Aeterna. rAthena
/// <c>skill.cpp:case PR_LEXAETERNA</c> applies
/// <see cref="StatusType.Aeterna"/> on the target — the next physical
/// or magical hit deals double damage, then the SC ends. Stops the
/// target from regenerating HP/SP while active.
///
/// Indefinite duration (rAthena: −1) — the SC only ends on a hit.
/// Skill is refused if the target is frozen / petrified (handled at
/// the cast-validation layer; not in scope for this plugin).
/// </summary>
public sealed class LexAeternaBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.PR_LEXAETERNA;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // Refuse re-cast — only one Aeterna may live on the target.
        // (rAthena: silently no-ops if already present.)
        if (ctx.Sc.Get(target, StatusType.Aeterna) != null) return true;

        // Permanent until consumed.
        ctx.Sc.Start(target, StatusType.Aeterna, val1: skillLevel, 0, 0, 0,
            durationMs: -1, source);
        return true;
    }
}
