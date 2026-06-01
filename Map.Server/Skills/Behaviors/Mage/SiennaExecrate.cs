using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SIENNAEXECRATE — Warlock Sienna Execrate. Manual port of
/// <c>rathena-fork/src/map/skills/mage/siennaexecrate.cpp</c>.
///
/// <para>Petrifies the primary target then chains the petrify across
/// the 3×3 splash (BCT_ENEMY) via <c>ctx.Entities.ForEachInRange</c>.
/// Success rate on the primary: <c>45 + 5*lv + job_level/4 %</c>.
/// Status-immune targets auto-skip.</para>
/// </summary>
public sealed class SiennaExecrate : SkillImpl
{
    private readonly Random _rng;

    public SiennaExecrate() : base(SkillIds.WL_SIENNAEXECRATE) => _rng = Random.Shared;

    public SiennaExecrate(Random? rng = null) : base(SkillIds.WL_SIENNAEXECRATE) => _rng = rng ?? Random.Shared;

    /// <summary>3×3 splash for the petrify chain (rAthena WL_SIENNAEXECRATE SplashRange).</summary>
    private const short SplashRadius = 1;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0)
            return;

        var jobLevel = src is PlayerEntity pc ? pc.JobLevel : 50;
        var rate = 45 + 5 * skillLevel + jobLevel / 4;
        if (_rng.Next(100) >= rate)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        // Primary target lands the petrify.
        ctx.Sc?.Start(target, StatusType.Stone, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 10_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Chain across the BCT_ENEMY splash centered on the primary target
        // (rAthena WL_SIENNAEXECRATE splash arm). Each victim eats the
        // same SC start; status-immune mobs skip.
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == target.Id || v.Id == src.Id) continue;
            if ((v.Stats.Mode & MobMode.StatusImmune) != 0) continue;
            ctx.Sc?.Start(v, StatusType.Stone, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 10_000, src);
        }
    }
}
