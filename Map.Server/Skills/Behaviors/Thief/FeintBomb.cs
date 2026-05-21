using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_FEINTBOMB — Feint Bomb. Manual port of
/// <c>rathena-fork/src/map/skills/thief/feintbomb.cpp</c>.
/// Drops a feint bomb at the cell + caster backslide + mob retarget.
/// Backslide / retarget are TODO.
/// </summary>
public sealed class FeintBomb : WeaponSkillImpl
{
    private readonly ISkillUnitService? _units;

    public FeintBomb() : base(SkillIds.SC_FEINTBOMB) { }

    public FeintBomb(ISkillUnitService? units = null) : base(SkillIds.SC_FEINTBOMB)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var jobLv = src is PlayerEntity p ? p.JobLevel : 50;
        return baseRatio + (-100 + (skillLevel + 1) * src.Stats.Dex / 2 * (jobLv / 10));
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
