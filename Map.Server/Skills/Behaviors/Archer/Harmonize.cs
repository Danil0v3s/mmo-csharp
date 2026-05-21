using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// MI_HARMONIZE — Minstrel Harmonize. Manual port of
/// <c>rathena-fork/src/map/skills/archer/harmonize.cpp</c>.
/// Applies SC_HARMONIZE on caster and target.
/// </summary>
public sealed class Harmonize : SkillImpl
{
    public Harmonize() : base(SkillIds.MI_HARMONIZE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        const int duration = 60_000;
        if (src.Id != target.Id)
        {
            ctx.Sc?.Start(src, StatusType.Harmonize, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
            ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        }
        ctx.Sc?.Start(target, StatusType.Harmonize, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
