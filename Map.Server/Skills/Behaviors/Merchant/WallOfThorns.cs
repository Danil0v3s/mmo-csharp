using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_WALLOFTHORN — Genetic Wall of Thorns. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/wallofthorns.cpp</c>.
/// Ratio <c>+10*lv</c>. CastendPos2 places the wall unit at (x, y).
/// Ammo is consumed up-front (handled outside this plugin).
/// </summary>
public sealed class WallOfThorns : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public WallOfThorns() : base(SkillIds.GN_WALLOFTHORN) { }

    public WallOfThorns(ISkillUnitService? units = null) : base(SkillIds.GN_WALLOFTHORN)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 10 * skillLevel;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
