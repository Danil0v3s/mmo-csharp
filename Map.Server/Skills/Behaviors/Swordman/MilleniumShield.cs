using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_MILLENNIUMSHIELD — Rune Knight Millennium Shield. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/milleniumshield.cpp</c>.
///
/// <para>Requires RK_RUNEMASTERY ≥ 9 (gated via
/// <see cref="IPlayerSkillService.CheckSkill"/>). Applies
/// SC_MILLENNIUMSHIELD.</para>
/// </summary>
public sealed class MilleniumShield : SkillImpl
{
    public MilleniumShield() : base(SkillIds.RK_MILLENNIUMSHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        if ((ctx.PlayerSkill?.CheckSkill(pc, SkillIds.RK_RUNEMASTERY) ?? 0) < 9) return;
        ctx.Sc?.Start(target, StatusType.Millenniumshield, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
