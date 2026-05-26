using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_SCREAM — Dancer Scream (Dazzler). Manual port of
/// <c>rathena-fork/src/map/skills/archer/dazzler.cpp</c>.
///
/// <para>Stuns enemies after a 3 s delay. Base accuracy
/// <c>(150 + 50*lv + 100) / 10 %</c> vs enemies, divided by 4 against
/// party members (same-party check via ctx.Party / PartyMap).</para>
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
        _timers?.Schedule(src, target, 3000, SkillId, skillLevel, (s, t, lv) => { /* delayed roll runs ApplyAdditionalEffects */ });
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 150 + 50 * skillLevel + 100;
        var duration = 15_000;
        // Party-member check: rAthena divides the rate by 4 and uses a
        // fixed 15 s duration. Without a shared party-id check on
        // non-PC targets the divide is gated on the player-vs-player
        // case.
        if (src is PlayerEntity pcSrc && target is PlayerEntity pcTgt
            && pcSrc.PartyId > 0 && pcSrc.PartyId == pcTgt.PartyId)
        {
            rate /= 4;
        }
        if (_rng.Next(1000) < rate)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: duration, src);
    }
}
