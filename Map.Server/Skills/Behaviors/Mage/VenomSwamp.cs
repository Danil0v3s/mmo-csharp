using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_VENOM_SWAMP — Elemental Master Venom Swamp. Manual port of
/// <c>rathena-fork/src/map/skills/mage/venomswamp.cpp</c>.
///
/// <para>Poison ground unit. Ratio: <c>+(-100 + 700 + 1100*lv) + 5*SPL</c>;
/// +<c>200*lv + 2*SPL</c> when SC_SUMMON_ELEMENTAL_SERPENS is active
/// on the caster. Splash victims roll 3 % SC_HANDICAPSTATE_DEADLYPOISON.</para>
/// </summary>
public sealed class VenomSwamp : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public VenomSwamp() : base(SkillIds.EM_VENOM_SWAMP) => _rng = Random.Shared;

    public VenomSwamp(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.EM_VENOM_SWAMP)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 700 + 1100 * skillLevel) + 5 * src.Stats.Spl;
        if (ctx.Sc?.Get(src, StatusType.SummonElementalSerpens) != null)
            ratio += 200 * skillLevel + 2 * src.Stats.Spl;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 3)
            ctx.Sc?.Start(target, StatusType.HandicapstateDeadlypoison, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
