using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_CONFLAGRATION — Elemental Master Conflagration. Manual port of
/// <c>rathena-fork/src/map/skills/mage/conflagration.cpp</c>.
///
/// <para>Ground unit drop. Ratio: <c>+(-100 + 700 + 1100*lv) + 5*SPL</c>,
/// with a +<c>200*lv + 2*SPL</c> bonus when SC_SUMMON_ELEMENTAL_ARDOR is
/// active on the caster. Splash victims roll a 3 % chance per tick to
/// receive SC_HANDICAPSTATE_CONFLAGRATION.</para>
/// </summary>
public sealed class Conflagration : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public Conflagration() : base(SkillIds.EM_CONFLAGRATION) => _rng = Random.Shared;

    public Conflagration(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.EM_CONFLAGRATION)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skillratio += -100 + 700 + 1100*lv + 5*SPL; +200*lv +2*SPL if SC_SUMMON_ELEMENTAL_ARDOR.
        var ratio = baseRatio + (-100 + 700 + 1100 * skillLevel) + 5 * src.Stats.Spl;
        if (ctx.Sc?.Get(src, StatusType.SummonElementalArdor) != null)
            ratio += 200 * skillLevel + 2 * src.Stats.Spl;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start with 3% rate, val1 = skill_lv, duration from skill_get_time2.
        if (_rng.Next(100) < 3)
            ctx.Sc?.Start(target, StatusType.HandicapstateConflagration, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
