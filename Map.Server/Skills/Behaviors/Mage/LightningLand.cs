using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_LIGHTNING_LAND — Elemental Master Lightning Land. Manual port of
/// <c>rathena-fork/src/map/skills/mage/lightningland.cpp</c>.
///
/// <para>Ground unit Wind magic. Ratio: <c>+(-100 + 700 + 1100*lv) + 5*SPL</c>;
/// SC_SUMMON_ELEMENTAL_PROCELLA caster bonus adds +<c>200*lv + 2*SPL</c>.
/// Splash victims roll 3 % SC_HANDICAPSTATE_LIGHTNINGSTRIKE.</para>
/// </summary>
public sealed class LightningLand : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;

    public LightningLand() : base(SkillIds.EM_LIGHTNING_LAND) => _rng = Random.Shared;

    public LightningLand(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.EM_LIGHTNING_LAND)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 700 + 1100 * skillLevel) + 5 * src.Stats.Spl;
        if (ctx.Sc?.Get(src, StatusType.SummonElementalProcella) != null)
            ratio += 200 * skillLevel + 2 * src.Stats.Spl;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 3)
            ctx.Sc?.Start(target, StatusType.HandicapstateLightningstrike, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
