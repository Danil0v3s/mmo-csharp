using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_VOLCANIC_ASH — Homunculus Volcanic Ash. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_volcanicash.cpp</c>.
/// Drops the ash cloud at (x, y).
/// </summary>
public sealed class VolcanicAsh : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public VolcanicAsh() : base(SkillIds.MH_VOLCANIC_ASH) { }

    public VolcanicAsh(ISkillUnitService? units = null) : base(SkillIds.MH_VOLCANIC_ASH)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
