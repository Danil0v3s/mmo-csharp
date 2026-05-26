using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_HAMMERFALL — Blacksmith Hammer Fall. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/hammerfall.cpp</c>.
/// Ground-targeted: every enemy in the splash radius rolls
/// <c>min(20+10*lv, 50+5*lv) %</c> SC_STUN after a 1 s timer.
/// </summary>
public sealed class HammerFall : SkillImpl
{
    private const short SplashRange = 3;
    private const int StunDelayMs = 1000;
    private const int StunDurationMs = 3000;

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
        _timers?.Schedule(src, target, StunDelayMs, SkillId, skillLevel, (s, t, lv) =>
        {
            if (_rng.Next(100) < rate)
                ctx.Sc?.Start(t, StatusType.Stun, val1: lv, 0, 0, 0, durationMs: StunDurationMs, s);
        });
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena ground-targeted variant: skill_castend_pos2 walks an
        // (x-i, y-i)..(x+i, y+i) box of BL_CHAR with BCT_ENEMY and runs
        // the no-damage castend on each — i.e. the per-victim stun roll.
        var rate = Math.Min(20 + 10 * skillLevel, 50 + 5 * skillLevel);
        var victims = ctx.Entities.ForEachInRange(src.MapId, x, y, SplashRange,
            EntityType.Mob | EntityType.Pc);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            var victim = v;
            _timers?.Schedule(src, victim, StunDelayMs, SkillId, skillLevel, (s, t, lv) =>
            {
                if (_rng.Next(100) < rate)
                    ctx.Sc?.Start(t, StatusType.Stun, val1: lv, 0, 0, 0, durationMs: StunDurationMs, s);
            });
        }
    }
}
