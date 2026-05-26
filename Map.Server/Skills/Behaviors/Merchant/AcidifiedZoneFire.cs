using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_ACIDIFIED_ZONE_FIRE — Biolo Acidified Zone (Fire). Manual port
/// of <c>rathena-fork/src/map/skills/merchant/acidifiedzonefire.cpp</c>.
/// Ground unit. Ratio: <c>+(-100 + 400*lv) + 5*POW</c>. When the
/// caster has SC_RESEARCHREPORT active the running ratio is scaled
/// <c>×1.5</c>; additionally when the target is Formless or Plant
/// the post-RR ratio is scaled <c>×1.5</c> again.
/// </summary>
public sealed class AcidifiedZoneFire : RecursiveDamageSplashSkillImpl
{
    private readonly ISkillUnitService? _units;

    public AcidifiedZoneFire() : base(SkillIds.BO_ACIDIFIED_ZONE_FIRE) { }

    public AcidifiedZoneFire(ISkillUnitService? units = null) : base(SkillIds.BO_ACIDIFIED_ZONE_FIRE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 400 * skillLevel) + 5 * src.Stats.Pow;
        if (ctx.Sc?.Get(src, StatusType.Researchreport) != null)
        {
            ratio += ratio * 50 / 100;
            if (target.Stats.Race == BattleRace.Formless || target.Stats.Race == BattleRace.Plant)
                ratio += ratio * 50 / 100;
        }
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
