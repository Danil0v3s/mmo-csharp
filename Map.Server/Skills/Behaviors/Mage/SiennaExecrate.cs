using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SIENNAEXECRATE — Warlock Sienna Execrate. Manual port of
/// <c>rathena-fork/src/map/skills/mage/siennaexecrate.cpp</c>.
///
/// <para>Petrifies the primary target then chains the petrify across
/// the splash (BCT_ENEMY). Success rate on the primary:
/// <c>45 + 5*lv + job_level/4 %</c>. Status-immune targets auto-skip.
/// Splash chain to other enemies is TODO until <c>map_foreachinrange</c>
/// is plumbed into per-skill hooks.</para>
/// </summary>
public sealed class SiennaExecrate : SkillImpl
{
    private readonly Random _rng;

    public SiennaExecrate() : base(SkillIds.WL_SIENNAEXECRATE) => _rng = Random.Shared;

    public SiennaExecrate(Random? rng = null) : base(SkillIds.WL_SIENNAEXECRATE) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0)
            return;

        var jobLevel = 50; // rAthena uses sd->status.job_level; not surfaced on Entity yet — fall back to 50.
        var rate = 45 + 5 * skillLevel + jobLevel / 4;
        if (_rng.Next(100) < rate)
        {
            ctx.Sc?.Start(target, StatusType.Stone, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 10_000, src);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            // TODO: chain petrify across splash via skill_area_sub.
        }
        else if (src is PlayerEntity sd)
        {
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
        }
    }
}
