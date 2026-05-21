using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_ICEBOUNDTRAP — Ranger Icebound Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/iceboundtrap.cpp</c>.
/// Trap unit + on-hit SC_FREEZING at <c>50 + 10*lv %</c>.
/// </summary>
public sealed class IceboundTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public IceboundTrap() : base(SkillIds.RA_ICEBOUNDTRAP) => _rng = Random.Shared;

    public IceboundTrap(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.RA_ICEBOUNDTRAP)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 50 + 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Freezing, val1: skillLevel, 0, 0, 0, durationMs: 8000, src);
    }
}
