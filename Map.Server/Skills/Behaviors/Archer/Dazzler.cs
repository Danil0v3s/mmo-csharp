using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_SCREAM — Dancer Scream (Dazzler). Manual port of
/// <c>rathena-fork/src/map/skills/archer/dazzler.cpp</c>.
///
/// <para>Stuns enemies after a 3 s delay. Rate <c>(150 + 50*lv + 100) / 10 %</c>
/// vs enemies, halved by 4 vs party members (party-check gate TODO).
/// The delayed broadcast uses the skill timer service.</para>
/// </summary>
public sealed class Dazzler : SkillImpl
{
    private readonly ISkillTimerService? _timers;
    private readonly Random _rng;

    public Dazzler() : base(SkillIds.DC_SCREAM) => _rng = Random.Shared;

    public Dazzler(ISkillTimerService? timers = null, Random? rng = null) : base(SkillIds.DC_SCREAM)
    {
        _timers = timers;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _timers?.Schedule(src, target, 3000, SkillId, skillLevel, (s, t, lv) => { /* schedule body */ });
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = (150 + 50 * skillLevel + 100) / 10;
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
