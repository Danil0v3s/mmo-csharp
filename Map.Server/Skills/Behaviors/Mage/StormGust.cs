using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_STORMGUST — Wizard Storm Gust. Ground unit placement (3-hit
/// staggered Water magic). Renewal ratio: <c>-30 + 50*lv</c>. Each
/// hit has a (65 - 5*lv) % chance to Freeze the target.
/// </summary>
public sealed class StormGust : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;
    public StormGust() : base(SkillIds.WZ_STORMGUST) => _rng = Random.Shared;
    public StormGust(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.WZ_STORMGUST)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-30 + 50 * skillLevel);
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var chance = 65 - 5 * skillLevel;
        if (chance > 0 && _rng.Next(100) < chance)
            ctx.Sc?.Start(target, StatusType.Freeze, val1: skillLevel, 0, 0, 0, durationMs: 8000, src);
    }
}
