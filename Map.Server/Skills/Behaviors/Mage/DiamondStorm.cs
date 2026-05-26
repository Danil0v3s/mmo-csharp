using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_DIAMOND_STORM — Elemental Master Diamond Storm. Manual port of
/// <c>rathena-fork/src/map/skills/mage/diamondstorm.cpp</c>.
///
/// <para>Ground unit Water magic. Ratio: <c>+(-100 + 500 + 2400*lv) + 5*SPL</c>,
/// plus a +<c>7300 + 200*lv + 5*SPL</c> bonus when SC_SUMMON_ELEMENTAL_DILUVIO
/// is active on the caster. Splash victims roll 5 %
/// SC_HANDICAPSTATE_FROSTBITE.</para>
/// </summary>
public sealed class DiamondStorm : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public DiamondStorm() : base(SkillIds.EM_DIAMOND_STORM) => _rng = Random.Shared;

    public DiamondStorm(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.EM_DIAMOND_STORM)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skillratio += -100 + 500 + 2400*lv + 5*SPL; +7300 + 200*lv + 5*SPL when SC_SUMMON_ELEMENTAL_DILUVIO.
        var ratio = baseRatio + (-100 + 500 + 2400 * skillLevel) + 5 * src.Stats.Spl;
        if (ctx.Sc?.Get(src, StatusType.SummonElementalDiluvio) != null)
            ratio += 7300 + 200 * skillLevel + 5 * src.Stats.Spl;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 5)
            ctx.Sc?.Start(target, StatusType.HandicapstateFrostbite, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
