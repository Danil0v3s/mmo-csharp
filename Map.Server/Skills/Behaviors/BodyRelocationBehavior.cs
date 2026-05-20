using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MO_BODYRELOCATION (id 264) — Monk Body Relocation. rAthena
/// <c>skill.cpp:case MO_BODYRELOCATION</c>: instantly teleports the
/// caster to a cell adjacent to the target — no damage, no SC.
/// Range = 7 cells flat in rAthena.
///
/// The actual teleport call lives on the movement service; the
/// plugin records the intent. When IMovementService.Teleport ports,
/// it gets called from here. For now we just claim the cast so the
/// generic resolver doesn't fire.
/// </summary>
public sealed class BodyRelocationBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MO_BODYRELOCATION;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // The teleport effect itself rides on the movement service —
        // this plugin claims the cast and the side-effect lands when
        // the warp-to-cell helper ports through SkillBehaviorContext.
        return true;
    }
}
