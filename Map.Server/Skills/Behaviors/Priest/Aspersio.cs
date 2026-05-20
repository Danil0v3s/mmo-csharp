using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Priest;

/// <summary>
/// PR_ASPERSIO — Priest Aspersio. Mirrors
/// <c>rathena-fork/src/map/skills/priest/aspersio.cpp</c>.
///
/// Apply <see cref="StatusType.Aspersio"/> on the target — weapon
/// becomes Holy element for the duration. Duration <c>180 + 60*lv</c>s.
/// </summary>
public sealed class Aspersio : SkillImpl
{
    public Aspersio() : base(SkillIds.PR_ASPERSIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var durationMs = 180_000 + 60_000 * skillLevel;
        ctx.Sc?.Start(target, StatusType.Aspersio, val1: skillLevel, 0, 0, 0, durationMs, src);
    }
}
