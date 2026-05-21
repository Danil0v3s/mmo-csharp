using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_AUTOCOUNTER — Knight Auto Counter. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/counterattack.cpp</c>.
/// Applies SC_AUTOCOUNTER to the caster and schedules a +100 ms
/// follow-up swing at the target.
/// </summary>
public sealed class CounterAttack : SkillImpl
{
    private readonly ISkillTimerService? _timers;

    public CounterAttack() : base(SkillIds.KN_AUTOCOUNTER) { }

    public CounterAttack(ISkillTimerService? timers = null) : base(SkillIds.KN_AUTOCOUNTER)
    {
        _timers = timers;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 20 / 100);

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Autocounter, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        _timers?.Schedule(src, target, delayMs: 100, SkillId, skillLevel,
            (s, t, lv) => ctx.Client?.BroadcastSkillNoDamage(s, t, SkillId, lv));
    }
}
