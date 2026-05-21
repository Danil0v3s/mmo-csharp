using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_SCRATCH — Summoner Scratch. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/scratch.cpp</c>.
/// Ratio <c>+(-50 + 50*lv)</c>. <c>10*lv + 70</c>% bleed on hit.
/// </summary>
public sealed class Scratch : RecursiveDamageSplashSkillImpl
{
    private readonly Random _rng;

    public Scratch() : base(SkillIds.SU_SCRATCH) => _rng = Random.Shared;

    public Scratch(Random? rng = null) : base(SkillIds.SU_SCRATCH)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-50 + 50 * skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 10 * skillLevel + 70)
            ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
    }
}
