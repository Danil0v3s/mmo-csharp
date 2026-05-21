using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_FIRINGTRAP — Ranger Firing Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/firingtrap.cpp</c>.
/// Trap unit + on-hit SC_BURNING at <c>50 + 10*lv %</c>.
/// </summary>
public sealed class FiringTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public FiringTrap() : base(SkillIds.RA_FIRINGTRAP) => _rng = Random.Shared;

    public FiringTrap(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.RA_FIRINGTRAP)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 50 + 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Burning, val1: skillLevel, val2: 1000, val3: (int)src.Id, 0, durationMs: 10_000, src);
    }
}
