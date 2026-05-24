using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_JYUSATSU — Illusion Death. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/illusiondeath.cpp</c>.
/// Chance <c>(45 + 5*lv + 5*lv - tgtInt/2)</c>% to inflict SC_HALLUCINATION
/// + percent damage; if target level &lt;= caster level, also SC_COMA.
/// </summary>
public sealed class IllusionDeath : SkillImpl
{
    public IllusionDeath() : base(SkillIds.KO_JYUSATSU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var chance = (45 + 5 * skillLevel) + skillLevel * 5 - target.Stats.IntStat / 2;
        if (System.Random.Shared.Next(100) < chance)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            // rAthena skill_db.yml `Status: Curse` (SC_CURSE) for KO_JYUSATSU;
            // Duration1 scales 6000..14000 ms per level.
            var sc1Duration = 6_000 + (skillLevel - 1) * 2_000;
            ctx.Sc?.Start(src, StatusType.Curse, val1: skillLevel, 0, 0, 0, durationMs: sc1Duration, src);
            if (target.Level <= src.Level)
                ctx.Sc?.Start(target, StatusType.Coma, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
            // Deferred: status_percent_damage(target, hp_rate, …, kill=false).
            // IStatusOpsService.PercentDamage exists but isn't currently exposed
            // through SkillBehaviorContext, so the HP-cut step waits for that
            // wiring; without it, the curse + optional coma still land.
        }
        else if (src is PlayerEntity sd)
        {
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
        }
    }
}
