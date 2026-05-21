using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_SATURDAY_NIGHT_FEVER — Minstrel/Wanderer Saturday Night Fever.
/// Manual port of <c>rathena-fork/src/map/skills/archer/saturdaynightfever.cpp</c>.
/// Per-target SC apply at <c>INT/6 + 4*lv %</c> (job_level + WM_LESSON
/// passive bonuses TODO).
/// </summary>
public sealed class SaturdayNightFever : SkillImpl
{
    private readonly Random _rng;

    public SaturdayNightFever() : base(SkillIds.WM_SATURDAY_NIGHT_FEVER) => _rng = Random.Shared;

    public SaturdayNightFever(Random? rng = null) : base(SkillIds.WM_SATURDAY_NIGHT_FEVER) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = src.Stats.IntStat / 6 + 4 * skillLevel;
        if (_rng.Next(100) < rate)
        {
            ctx.Sc?.Start(target, StatusType.Saturdaynightfever, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        }
    }
}
