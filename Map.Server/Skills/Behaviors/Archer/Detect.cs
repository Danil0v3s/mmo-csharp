using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_DETECTING — Hunter Detect (skill.cpp:HT_DETECTING arm). Reveals
/// every hidden / cloaked / chasewalk character in a 7×7 splash by
/// ending the relevant SCs on each victim. The trap-reveal sweep
/// (BL_SKILL units) is a separate concern — BL_SKILL splash isn't
/// part of <see cref="IEntityRegistry"/> today; the SC reveal half is
/// the behavior most builds care about.
/// </summary>
public sealed class Detect : SkillImpl
{
    private const short SplashRadius = 3; // rAthena: 7×7 (radius 3).

    public Detect() : base(SkillIds.HT_DETECTING) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var victims = ctx.Entities.ForEachInRange(src.MapId, x, y, SplashRadius,
            EntityType.Pc | EntityType.Mob);
        foreach (var v in victims)
        {
            ctx.Sc.End(v, StatusType.Hiding);
            ctx.Sc.End(v, StatusType.Cloaking);
            ctx.Sc.End(v, StatusType.Chasewalk);
            ctx.Sc.End(v, StatusType.Cloakingexceed);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
