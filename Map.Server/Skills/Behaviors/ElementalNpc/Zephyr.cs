using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_ZEPHYR — Elemental Zephyr. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/zephyr.cpp</c>.
/// Drops a zephyr unit at the target's cell.
/// </summary>
public sealed class Zephyr : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Zephyr() : base(SkillIds.EL_ZEPHYR) { }

    public Zephyr(ISkillUnitService? units = null) : base(SkillIds.EL_ZEPHYR)
    {
        _units = units;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, target.X, target.Y);
}
