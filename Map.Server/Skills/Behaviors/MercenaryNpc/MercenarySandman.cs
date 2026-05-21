using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_SANDMAN — Mercenary Sandman. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_sandman.cpp</c>.
/// Drops the trap; on hit (10*lv + 40)% to sleep.
/// </summary>
public sealed class MercenarySandman : SkillImpl
{
    private readonly Random _rng;
    private readonly ISkillUnitService? _units;

    public MercenarySandman() : base(SkillIds.MA_SANDMAN) => _rng = Random.Shared;

    public MercenarySandman(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.MA_SANDMAN)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 10 * skillLevel + 40)
            ctx.Sc?.Start(target, StatusType.Sleep, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
