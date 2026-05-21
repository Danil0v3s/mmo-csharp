using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_FORCEOFVANGUARD — Royal Guard Force of Vanguard. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/forceofvanguard.cpp</c>.
/// Toggles SC_FORCEOFVANGUARD: dispels if active, otherwise starts it.
/// </summary>
public sealed class ForceOfVanguard : SkillImpl
{
    public ForceOfVanguard() : base(SkillIds.LG_FORCEOFVANGUARD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Forceofvanguard) != null)
            ctx.Sc.End(target, StatusType.Forceofvanguard);
        else
            ctx.Sc?.Start(target, StatusType.Forceofvanguard, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
