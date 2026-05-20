using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Priest;

/// <summary>
/// PR_IMPOSITIO — Priest Impositio Manus. Mirrors
/// <c>rathena-fork/src/map/skills/priest/impositio.cpp</c>.
///
/// Apply <see cref="StatusType.Impositio"/> with <c>Val1 = lv * 5</c>
/// (flat ATK boost). Duration 60 s renewal.
/// </summary>
public sealed class Impositio : SkillImpl
{
    public Impositio() : base(SkillIds.PR_IMPOSITIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Impositio, val1: skillLevel * 5, 0, 0, 0,
            durationMs: 60_000, src);
    }
}
