using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_ABUNDANCE — Rune Knight Abundance. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/abundance.cpp</c>.
///
/// <para>Requires RK_RUNEMASTERY ≥ 6 (gated via
/// <see cref="IPlayerSkillService.CheckSkill"/>). Applies SC_ABUNDANCE
/// to the target on success.</para>
/// </summary>
public sealed class Abundance : SkillImpl
{
    public Abundance() : base(SkillIds.RK_ABUNDANCE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        if ((ctx.PlayerSkill?.CheckSkill(sd, SkillIds.RK_RUNEMASTERY) ?? 0) < 6) return;
        ctx.Sc?.Start(target, StatusType.Abundance, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
