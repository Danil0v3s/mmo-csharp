using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_CRUSHSTRIKE — Rune Knight Crush Strike. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/crushstrike.cpp</c>.
/// Requires RK_RUNEMASTERY ≥ 7 (TODO). Applies SC_CRUSHSTRIKE.
/// </summary>
public sealed class CrushStrike : SkillImpl
{
    public CrushStrike() : base(SkillIds.RK_CRUSHSTRIKE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: gate on pc_checkskill(sd, RK_RUNEMASTERY) >= 7.
        ctx.Sc?.Start(target, StatusType.Crushstrike, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
