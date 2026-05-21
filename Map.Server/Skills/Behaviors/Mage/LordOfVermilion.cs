using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_VERMILION — Wizard Lord of Vermilion. Ground unit placement
/// (Wind multi-hit zone). PC ratio: +300 + 100*lv; mob ratio:
/// 20*lv-20. Blind chance: 10 + 5*lv %.
/// </summary>
public sealed class LordOfVermilion : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly Random _rng;
    public LordOfVermilion() : base(SkillIds.WZ_VERMILION) => _rng = Random.Shared;
    public LordOfVermilion(ISkillUnitService? units = null, Random? rng = null) : base(SkillIds.WZ_VERMILION)
    {
        _units = units;
        _rng = rng ?? Random.Shared;
    }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => src is PlayerEntity
            ? baseRatio + 300 + skillLevel * 100
            : baseRatio + 20 * skillLevel - 20;
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var chance = 10 + 5 * skillLevel;
        if (_rng.Next(100) < chance)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
