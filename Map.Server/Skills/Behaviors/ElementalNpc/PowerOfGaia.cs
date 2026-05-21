using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_POWER_OF_GAIA — Elemental Power Of Gaia. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/powerofgaia.cpp</c>.
/// Drops a Power Of Gaia unit at the target's cell.
/// </summary>
public sealed class PowerOfGaia : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public PowerOfGaia() : base(SkillIds.EL_POWER_OF_GAIA) { }

    public PowerOfGaia(ISkillUnitService? units = null) : base(SkillIds.EL_POWER_OF_GAIA)
    {
        _units = units;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, target.X, target.Y);
}
