using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_CLUSTERBOMB — Ranger Cluster Bomb. Manual port of
/// <c>rathena-fork/src/map/skills/archer/clusterbomb.cpp</c>.
/// Drops a damage-trap with ratio <c>+(100 + 100*lv)</c>.
/// </summary>
public sealed class ClusterBomb : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ClusterBomb() : base(SkillIds.RA_CLUSTERBOMB) { }

    public ClusterBomb(ISkillUnitService? units = null) : base(SkillIds.RA_CLUSTERBOMB)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (100 + 100 * skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
