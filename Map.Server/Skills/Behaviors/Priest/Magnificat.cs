using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Priest;

/// <summary>
/// PR_MAGNIFICAT — Priest Magnificat. Mirrors
/// <c>rathena-fork/src/map/skills/priest/magnificat.cpp</c>.
///
/// Apply <see cref="StatusType.Magnificat"/> on the caster (party
/// broadcast pending). Doubles SP regen via NaturalHealService
/// overlay. Duration <c>30 * lv</c> seconds.
/// </summary>
public sealed class Magnificat : SkillImpl
{
    public Magnificat() : base(SkillIds.PR_MAGNIFICAT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Self-target regardless of which player the client picked.
        ctx.Sc?.Start(src, StatusType.Magnificat, val1: skillLevel, 0, 0, 0,
            durationMs: 30_000 * skillLevel, src);
    }
}
