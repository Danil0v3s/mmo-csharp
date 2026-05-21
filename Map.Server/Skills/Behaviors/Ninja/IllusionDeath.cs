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
            // TODO: percent_damage(target, hp*lv*5%); apply Jyusatsu status.
            if (target.Level <= src.Level)
                ctx.Sc?.Start(target, StatusType.Coma, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        }
        else if (src is PlayerEntity sd)
        {
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
        }
    }
}
