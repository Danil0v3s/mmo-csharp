using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_GLOOMYDAY — Minstrel/Wanderer Gloomy Day. Manual port of
/// <c>rathena-fork/src/map/skills/archer/gloomyday.cpp</c>.
/// Applies SC_GLOOMYDAY (or SC_GLOOMYDAY_SK against shield/charge
/// classes — player skill-tree check TODO; we apply the standard SC).
/// </summary>
public sealed class GloomyDay : SkillImpl
{
    public GloomyDay() : base(SkillIds.WM_GLOOMYDAY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Gloomyday, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
