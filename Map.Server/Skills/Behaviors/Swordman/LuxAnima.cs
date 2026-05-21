using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_LUXANIMA — Rune Knight Lux Anima. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/luxanima.cpp</c>.
/// Strips the target's bonus_script buffs (SCCB_LUXANIMA) and applies
/// SC_LUXANIMA. Bonus-script clear is TODO.
/// </summary>
public sealed class LuxAnima : SkillImpl
{
    public LuxAnima() : base(SkillIds.RK_LUXANIMA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: status_change_clear_buffs(target, SCCB_LUXANIMA) on PlayerEntity bonus scripts.
        ctx.Sc?.Start(target, StatusType.Luxanima, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
