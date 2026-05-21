using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_COLDSLOWER — Mechanic Cold Slower. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/coldslower.cpp</c>.
/// Ratio: <c>+(200 + 300*lv)</c>. On-hit SC_FREEZE at <c>10*lv %</c>;
/// falls back to SC_FREEZING at <c>20 + 10*lv %</c>.
/// </summary>
public sealed class ColdSlower : RecursiveDamageSplashSkillImpl
{
    private readonly Random _rng;

    public ColdSlower() : base(SkillIds.NC_COLDSLOWER) => _rng = Random.Shared;

    public ColdSlower(Random? rng = null) : base(SkillIds.NC_COLDSLOWER) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200 + 300 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 10 * skillLevel)
        {
            ctx.Sc?.Start(target, StatusType.Freeze, val1: skillLevel, 0, 0, 0, durationMs: 8000, src);
        }
        else if (_rng.Next(100) < 20 + 10 * skillLevel)
        {
            ctx.Sc?.Start(target, StatusType.Freezing, val1: skillLevel, 0, 0, 0, durationMs: 8000, src);
        }
    }
}
