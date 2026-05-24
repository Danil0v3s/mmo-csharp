using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_POWEROFFLOCK — Summoner Power of Flock. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/powerofflock.cpp</c>.
/// Applies SC_FEAR + SC_FREEZE to every enemy in the splash radius.
/// </summary>
public sealed class PowerofFlock : SkillImpl
{
    public PowerofFlock() : base(SkillIds.SU_POWEROFFLOCK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // skill_db SU_POWEROFFLOCK splash radius = 9. Iterate every
        // mob in range and apply both SCs.
        const short splashRange = 9;
        var victims = ctx.Entities.ForEachInRange(src.MapId, target.X, target.Y, splashRange, EntityType.Mob);
        foreach (var bl in victims)
        {
            ctx.Sc?.Start(bl, StatusType.Fear, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
            ctx.Sc?.Start(bl, StatusType.Freeze, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        }
    }
}
