using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// TF_HIDING (id 51) — Thief Hiding. rAthena
/// <c>skill.cpp:case TF_HIDING</c> toggles <see cref="StatusType.Hiding"/>
/// on the caster. Each level extends SP-drain interval (rAthena: SP cost
/// every <c>5 - lv*0.5</c> seconds, drains while hiding); we attach the
/// SC for the per-level duration and let the SP drain port later.
/// </summary>
public sealed class HidingBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.TF_HIDING;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // Toggle semantics — re-cast while hidden ends the SC instead of
        // re-applying. rAthena uses the same skill id for both directions.
        if (ctx.Sc.Get(source, StatusType.Hiding) != null)
        {
            ctx.Sc.End(source, StatusType.Hiding);
            return true;
        }

        var durationMs = 60_000 + 30_000 * skillLevel; // rAthena: ramps up by level.
        ctx.Sc.Start(source, StatusType.Hiding, val1: skillLevel, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
