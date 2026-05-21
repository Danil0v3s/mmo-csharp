using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// SM_AUTOBERSERK — Swordman Auto Berserk toggle. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/autoberserk.cpp</c>.
/// Toggles SC_AUTOBERSERK on/off (60s when starting).
/// </summary>
public sealed class AutoBerserk : SkillImpl
{
    public AutoBerserk() : base(SkillIds.SM_AUTOBERSERK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null && ctx.Sc.Get(target, StatusType.Autoberserk) != null)
            ctx.Sc.End(target, StatusType.Autoberserk);
        else
            ctx.Sc?.Start(target, StatusType.Autoberserk, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
