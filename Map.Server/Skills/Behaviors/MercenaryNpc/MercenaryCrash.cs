using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_CRASH — Mercenary Crash. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_crash.cpp</c>.
/// Ratio <c>+10*lv</c>. (6*lv)% stun on hit.
/// </summary>
public sealed class MercenaryCrash : WeaponSkillImpl
{
    private readonly Random _rng;

    public MercenaryCrash() : base(SkillIds.MER_CRASH) => _rng = Random.Shared;

    public MercenaryCrash(Random? rng = null) : base(SkillIds.MER_CRASH)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 6 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
