using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_LULLABY_DEEPSLEEP — Minstrel/Wanderer Deep Sleep Lullaby. Manual
/// port of <c>rathena-fork/src/map/skills/archer/deepsleeplullaby.cpp</c>.
///
/// <para>Splash sleep: caster broadcasts then every nearby BL_CHAR
/// rolls for SC_DEEPSLEEP. Rate = <c>4*lv + WM_LESSON*2 + caster_lv/15
/// + job_level/5</c>; per-target duration deducts <c>INT*50 + lvl*50</c>
/// from the base 30 s.</para>
/// </summary>
public sealed class DeepSleepLullaby : SkillImpl
{
    private readonly Random _rng;

    public DeepSleepLullaby() : base(SkillIds.WM_LULLABY_DEEPSLEEP) => _rng = Random.Shared;
    public DeepSleepLullaby(Random? rng = null) : base(SkillIds.WM_LULLABY_DEEPSLEEP) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        const short splash = 9;

        var lesson = (src is PlayerEntity pc) ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0) : 0;
        var job = (src is PlayerEntity pc2) ? pc2.JobLevel : 50;
        var baseRate = 4 * skillLevel + lesson * 2 + src.Level / 15 + job / 5;

        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y, splash, EntityType.Mob | EntityType.Pc);
        foreach (var bl in victims)
        {
            if (bl.Id.Value == src.Id.Value) continue;
            if (_rng.Next(100) >= baseRate) continue;
            var duration = Math.Max(1000, 30_000 - (bl.Stats.IntStat * 50 + bl.Level * 50));
            ctx.Sc?.Start(bl, StatusType.Deepsleep, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
        }
    }
}
