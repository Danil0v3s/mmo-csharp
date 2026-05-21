using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_ACIDIFIED_ZONE_WIND — Biolo Acidified Zone (Wind). Manual port
/// of <c>rathena-fork/src/map/skills/merchant/acidifiedzonewind.cpp</c>.
/// Same shape as <see cref="AcidifiedZoneFire"/>.
/// </summary>
public sealed class AcidifiedZoneWind : RecursiveDamageSplashSkillImpl
{
    private readonly ISkillUnitService? _units;

    public AcidifiedZoneWind() : base(SkillIds.BO_ACIDIFIED_ZONE_WIND) { }

    public AcidifiedZoneWind(ISkillUnitService? units = null) : base(SkillIds.BO_ACIDIFIED_ZONE_WIND)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
