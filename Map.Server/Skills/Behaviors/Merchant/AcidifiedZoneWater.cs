using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_ACIDIFIED_ZONE_WATER — Biolo Acidified Zone (Water). Manual
/// port of <c>rathena-fork/src/map/skills/merchant/acidifiedzonewater.cpp</c>.
/// Same shape as <see cref="AcidifiedZoneFire"/>.
/// </summary>
public sealed class AcidifiedZoneWater : RecursiveDamageSplashSkillImpl
{
    private readonly ISkillUnitService? _units;

    public AcidifiedZoneWater() : base(SkillIds.BO_ACIDIFIED_ZONE_WATER) { }

    public AcidifiedZoneWater(ISkillUnitService? units = null) : base(SkillIds.BO_ACIDIFIED_ZONE_WATER)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
