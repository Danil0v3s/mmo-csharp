using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_ARRULLO — Sorcerer Arrullo. Manual port of
/// <c>rathena-fork/src/map/skills/mage/arrullo.cpp</c>.
///
/// <para>AOE deep-sleep. Per-victim rate:
/// <c>(15 + 5*lv) + caster_INT/5 + job_level/5 - target_INT/6 - target_LUK/10</c>.
/// 5×5 splash dispatch fans out via <c>ctx.Entities.ForEachInRange</c>
/// on the cast XY; mob casters fall back to job_level=50 for the
/// caster term.</para>
/// </summary>
public sealed class Arrullo : SkillImpl
{
    private readonly Random _rng;

    public Arrullo() : base(SkillIds.SO_ARRULLO) => _rng = Random.Shared;

    public Arrullo(Random? rng = null) : base(SkillIds.SO_ARRULLO) => _rng = rng ?? Random.Shared;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena map_foreachinallarea around (x,y) with a 5×5 ground
        // splash (skill_db SplashRange 2). Every enemy in the cell box
        // rolls the deep-sleep chance.
        const short SplashRadius = 2;
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        var victims = ctx.Entities.ForEachInRange(src.MapId, x, y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            RollSleep(src, v, skillLevel, ctx);
        }
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        RollSleep(src, target, skillLevel, ctx);
    }

    private void RollSleep(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var jobLevel = src is PlayerEntity pc ? pc.JobLevel : 50;
        var rate = (15 + 5 * skillLevel)
            + src.Stats.IntStat / 5
            + jobLevel / 5
            - target.Stats.IntStat / 6
            - target.Stats.Luk / 10;
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Deepsleep, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
