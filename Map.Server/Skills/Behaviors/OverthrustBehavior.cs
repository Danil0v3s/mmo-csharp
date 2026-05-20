using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// BS_OVERTHRUST (id 113) — Blacksmith Over Thrust. rAthena
/// <c>skill.cpp:case BS_OVERTHRUST</c> applies
/// <see cref="StatusType.Overthrust"/> on the caster: weapon ATK
/// +<c>5 * lv</c>% but the weapon takes a 1 % break-chance per
/// hit (break path ports later — see SkillSideEffectService.BreakEquip).
///
/// Duration <c>180_000</c> ms flat in renewal.
/// </summary>
public sealed class OverthrustBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.BS_OVERTHRUST;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // Val1 = ATK boost % the SC handler will fold into the bonus path.
        ctx.Sc.Start(source, StatusType.Overthrust, val1: 5 * skillLevel, 0, 0, 0,
            durationMs: 180_000, source);
        return true;
    }
}
