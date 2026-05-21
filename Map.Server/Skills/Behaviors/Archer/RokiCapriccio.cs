using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_ROKI_CAPRICCIO — Trouvere/Troubadour Roki Capriccio. Manual
/// port of <c>rathena-fork/src/map/skills/archer/rokicapriccio.cpp</c>.
/// Splash debuff: SC_CONFUSION + SC_HANDICAPSTATE_MISFORTUNE.
/// Splash + partner doubling TODO.
/// </summary>
public sealed class RokiCapriccio : SkillImpl
{
    private readonly Random _rng;

    public RokiCapriccio() : base(SkillIds.TR_ROKI_CAPRICCIO) => _rng = Random.Shared;

    public RokiCapriccio(Random? rng = null) : base(SkillIds.TR_ROKI_CAPRICCIO) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        if (_rng.Next(100) < 4 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Confusion, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        if (_rng.Next(100) < 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.HandicapstateMisfortune, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
