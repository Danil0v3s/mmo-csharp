using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_LUXANIMA — Rune Knight Lux Anima (skill.cpp:11297). Calls
/// <c>status_change_clear_buffs(target, SCCB_LUXANIMA)</c> to purge the
/// target's bonus_script row, then applies SC_LUXANIMA.
/// </summary>
public sealed class LuxAnima : SkillImpl
{
    public LuxAnima() : base(SkillIds.RK_LUXANIMA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.ClearBuffs(target, SccbFlag.Luxanima);
        ctx.Sc?.Start(target, StatusType.Luxanima, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
