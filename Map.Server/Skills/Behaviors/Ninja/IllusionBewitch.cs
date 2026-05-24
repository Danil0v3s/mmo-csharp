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
        // Cache caster origin BEFORE the swap.
        var (origX, origY) = (src.X, src.Y);
        ctx.Sc?.Start(src, StatusType.Confusion, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        ctx.Sc?.Start(target, StatusType.Confusion, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        // rAthena: skill_check_unit_movepos(5, src, target->x, target->y, 0, 0)
        // then unit_movepos(target, origX, origY, 0, 0). C# MovePos / CheckUnitMovePos
        // run the same teleport contract.
        if (ctx.UnitOps?.CheckUnitMovePos(src, target.X, target.Y, 0) == true)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
            ctx.UnitOps?.MovePos(target, origX, origY, 0, false);
            // Deferred: map_foreachinallrange(unit_changetarget, src, AREA_SIZE, BL_CHAR, src, target)
            // — retarget every mob currently chasing the caster onto the
            // victim. No foreachinrange retarget sweep is exposed by
            // IMobChangeTargetService yet.
        }
        else
        {
            ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        }
    }
}
