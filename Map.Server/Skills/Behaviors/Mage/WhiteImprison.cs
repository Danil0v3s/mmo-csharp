using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_WHITEIMPRISON — Warlock White Imprison. Manual port of
/// <c>rathena-fork/src/map/skills/mage/whiteimprison.cpp</c>.
///
/// <para>Imprisons the target in a Ghost-element shell. Rates:
/// 100 % on self, <c>job_level/4 + 20 + 10*lv %</c> vs players,
/// <c>job_level/4 + 40 + 10*lv %</c> vs mobs. Fails on bosses and
/// status-immune targets. Caster also takes a 4 s skill block. The
/// block timer + battle_check_target gate aren't wired here — we
/// gate by status-immunity only.</para>
/// </summary>
public sealed class WhiteImprison : SkillImpl
{
    private readonly Random _rng;

    public WhiteImprison() : base(SkillIds.WL_WHITEIMPRISON) => _rng = Random.Shared;

    public WhiteImprison(Random? rng = null) : base(SkillIds.WL_WHITEIMPRISON) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }

        var jobLevel = 50;
        int rate;
        int durationMs;
        if (src.Id == target.Id)
        {
            rate = 100;
            durationMs = 5000;
        }
        else if (target is PlayerEntity)
        {
            rate = jobLevel / 4 + 20 + 10 * skillLevel;
            durationMs = 8000;
        }
        else
        {
            rate = jobLevel / 4 + 40 + 10 * skillLevel;
            durationMs = 10_000;
        }

        if (_rng.Next(100) >= rate)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Sc?.Start(target, StatusType.Whiteimprison, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
