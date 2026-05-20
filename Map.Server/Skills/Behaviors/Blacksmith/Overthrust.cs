using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Blacksmith;

/// <summary>
/// BS_OVERTHRUST — Blacksmith Over Thrust. Mirrors
/// <c>rathena-fork/src/map/skills/blacksmith/overthrust.cpp</c>.
///
/// Apply <see cref="StatusType.Overthrust"/> on the caster:
/// weapon ATK +5*lv %, 1 % break-chance per hit. Duration 180 s.
/// </summary>
public sealed class Overthrust : SkillImpl
{
    public Overthrust() : base(SkillIds.BS_OVERTHRUST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Overthrust, val1: 5 * skillLevel, 0, 0, 0,
            durationMs: 180_000, src);
    }
}
