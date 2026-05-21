using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_KAENSIN — Crimson Fire Formation. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/crimsonfireformation.cpp</c>.
/// Drops a fire-formation unit at the targeted cell; -50 base ratio
/// + 20*charm when fire charms are held (charm bonus TODO).
/// </summary>
public sealed class CrimsonFireFormation : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CrimsonFireFormation() : base(SkillIds.NJ_KAENSIN) { }

    public CrimsonFireFormation(ISkillUnitService? units = null) : base(SkillIds.NJ_KAENSIN)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio - 50;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
