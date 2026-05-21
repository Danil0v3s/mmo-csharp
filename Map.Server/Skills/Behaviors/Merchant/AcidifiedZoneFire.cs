using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_ACIDIFIED_ZONE_FIRE — Biolo Acidified Zone (Fire). Manual port
/// of <c>rathena-fork/src/map/skills/merchant/acidifiedzonefire.cpp</c>.
/// Ground unit. Ratio: <c>+(-100 + 400*lv) + 5*POW</c> (Research Report
/// + Formless/Plant bonuses TODO).
/// </summary>
public sealed class AcidifiedZoneFire : RecursiveDamageSplashSkillImpl
{
    private readonly ISkillUnitService? _units;

    public AcidifiedZoneFire() : base(SkillIds.BO_ACIDIFIED_ZONE_FIRE) { }

    public AcidifiedZoneFire(ISkillUnitService? units = null) : base(SkillIds.BO_ACIDIFIED_ZONE_FIRE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
