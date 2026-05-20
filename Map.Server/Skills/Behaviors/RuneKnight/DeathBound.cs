using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.RuneKnight;

/// <summary>
/// RK_DEATHBOUND — Rune Knight Death Bound. Mirrors
/// <c>rathena-fork/src/map/skills/runeknight/deathbound.cpp</c>.
///
/// Apply <see cref="StatusType.Deathbound"/> on caster — next
/// incoming physical hit is reflected back at multiplied damage.
/// Duration <c>10 + 5*lv</c> seconds.
/// </summary>
public sealed class DeathBound : SkillImpl
{
    public DeathBound() : base(SkillIds.RK_DEATHBOUND) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Deathbound, val1: skillLevel, 0, 0, 0,
            durationMs: (10 + 5 * skillLevel) * 1000, src);
    }
}
