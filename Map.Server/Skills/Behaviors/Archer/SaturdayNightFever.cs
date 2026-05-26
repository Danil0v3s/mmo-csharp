using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_SATURDAY_NIGHT_FEVER — Minstrel/Wanderer Saturday Night Fever.
/// Manual port of <c>rathena-fork/src/map/skills/archer/saturdaynightfever.cpp</c>.
///
/// <para>Splash SC apply at <c>INT/6 + job_level/5 + 4*lv +
/// WM_LESSON %</c>. Every nearby PC enemy rolls the SC.</para>
/// </summary>
public sealed class SaturdayNightFever : SkillImpl
{
    private readonly Random _rng;

    public SaturdayNightFever() : base(SkillIds.WM_SATURDAY_NIGHT_FEVER) => _rng = Random.Shared;
    public SaturdayNightFever(Random? rng = null) : base(SkillIds.WM_SATURDAY_NIGHT_FEVER) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 0;
        var job = (src is PlayerEntity pc2) ? pc2.JobLevel : 50;
        var rate = src.Stats.IntStat / 6 + job / 5 + 4 * skillLevel + lesson;
        if (_rng.Next(100) >= rate) return;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        const short splash = 7;
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, splash, EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id.Value == src.Id.Value) continue;
            ctx.Sc?.Start(v, StatusType.Saturdaynightfever, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        }
    }
}
