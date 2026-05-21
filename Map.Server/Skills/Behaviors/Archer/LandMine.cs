using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_LANDMINE — Hunter Land Mine. Manual port of
/// <c>rathena-fork/src/map/skills/archer/landmine.cpp</c>.
/// Trap + on-hit SC_STUN at 10 %.
/// </summary>
public sealed class LandMine : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public LandMine() : base(SkillIds.HT_LANDMINE) => _rng = Random.Shared;

    public LandMine(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.HT_LANDMINE)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 10)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 3000, src);
    }
}
