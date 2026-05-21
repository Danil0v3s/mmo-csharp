using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_WATER_BARRIER — Elemental Water Barrier. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/waterbarrier.cpp</c>.
/// Drops a water barrier unit at the target's cell.
/// </summary>
public sealed class WaterBarrier : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public WaterBarrier() : base(SkillIds.EL_WATER_BARRIER) { }

    public WaterBarrier(ISkillUnitService? units = null) : base(SkillIds.EL_WATER_BARRIER)
    {
        _units = units;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, target.X, target.Y);
}
