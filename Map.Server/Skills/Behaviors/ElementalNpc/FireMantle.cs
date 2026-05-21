using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_FIRE_MANTLE — Elemental Fire Mantle. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/firemantle.cpp</c>.
/// Drops a fire-mantle unit at the target's cell; +900 ratio when the
/// unit lands a hit.
/// </summary>
public sealed class FireMantle : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public FireMantle() : base(SkillIds.EL_FIRE_MANTLE) { }

    public FireMantle(ISkillUnitService? units = null) : base(SkillIds.EL_FIRE_MANTLE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 900;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, target.X, target.Y);
}
