using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_CRACKER — Gunslinger Cracker. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/cracker.cpp</c>.
/// Rate = max(30, 65 - 5*chebyshev-distance). Stuns the target.
/// </summary>
public sealed class Cracker : SkillImpl
{
    private readonly Random _rng;

    public Cracker() : base(SkillIds.GS_CRACKER) => _rng = Random.Shared;

    public Cracker(Random? rng = null) : base(SkillIds.GS_CRACKER)
        => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena skill.cpp: rate = 65 - 5*distance, clamped >= 30.
        var distance = Math.Max(Math.Abs(src.X - target.X), Math.Abs(src.Y - target.Y));
        var rate = Math.Max(30, 65 - 5 * distance);
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
