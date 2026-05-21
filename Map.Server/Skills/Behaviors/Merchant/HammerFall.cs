using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_HAMMERFALL — Blacksmith Hammer Fall. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/hammerfall.cpp</c>.
/// After a 1 s delay, victims roll <c>min(20+10*lv, 50+5*lv) %</c>
/// SC_STUN. Splash dispatch TODO; named target gets the roll.
/// </summary>
public sealed class HammerFall : SkillImpl
{
    private readonly ISkillTimerService? _timers;
    private readonly Random _rng;

    public HammerFall() : base(SkillIds.BS_HAMMERFALL) => _rng = Random.Shared;

    public HammerFall(ISkillTimerService? timers = null, Random? rng = null) : base(SkillIds.BS_HAMMERFALL)
    {
        _timers = timers;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = Math.Min(20 + 10 * skillLevel, 50 + 5 * skillLevel);
        _timers?.Schedule(src, target, 1000, SkillId, skillLevel, (s, t, lv) =>
        {
            if (_rng.Next(100) < rate)
                ctx.Sc?.Start(t, StatusType.Stun, val1: lv, 0, 0, 0, durationMs: 3000, s);
        });
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
