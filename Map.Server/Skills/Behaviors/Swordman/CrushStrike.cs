using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_CRUSHSTRIKE — Rune Knight Crush Strike. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/crushstrike.cpp</c>.
///
/// <para>Requires RK_RUNEMASTERY ≥ 7 (gated via
/// <see cref="IPlayerSkillService.CheckSkill"/>). Applies SC_CRUSHSTRIKE.</para>
/// </summary>
public sealed class CrushStrike : SkillImpl
{
    public CrushStrike() : base(SkillIds.RK_CRUSHSTRIKE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        if ((ctx.PlayerSkill?.CheckSkill(pc, SkillIds.RK_RUNEMASTERY) ?? 0) < 7) return;
        ctx.Sc?.Start(target, StatusType.Crushstrike, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
