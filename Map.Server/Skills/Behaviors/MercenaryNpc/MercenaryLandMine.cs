using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_LANDMINE — Mercenary Land Mine. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_landmine.cpp</c>.
/// Drops the unit; 10% stun on hit.
/// </summary>
public sealed class MercenaryLandMine : SkillImpl
{
    private readonly Random _rng;
    private readonly ISkillUnitService? _units;

    public MercenaryLandMine() : base(SkillIds.MA_LANDMINE) => _rng = Random.Shared;

    public MercenaryLandMine(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.MA_LANDMINE)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 10)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
