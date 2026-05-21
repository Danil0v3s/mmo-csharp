using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_GENWAKU — Illusion Bewitch. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/illusionbewitch.cpp</c>.
/// Inflicts SC_CONFUSION on caster (2500/10000) + target (7500/10000)
/// and swaps positions. Position swap is TODO (skill_check_unit_movepos).
/// </summary>
public sealed class IllusionBewitch : SkillImpl
{
    public IllusionBewitch() : base(SkillIds.KO_GENWAKU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var roll = System.Random.Shared.Next(100);
        if (roll > (45 + 5 * skillLevel) - target.Stats.IntStat / 10)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Sc?.Start(src, StatusType.Confusion, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        ctx.Sc?.Start(target, StatusType.Confusion, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // TODO: position swap + clif_blown + retarget mobs.
    }
}
