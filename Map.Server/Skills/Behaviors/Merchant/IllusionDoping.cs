using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_ILLUSIONDOPING — Genetic Illusion Doping. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/illusiondoping.cpp</c>.
/// On-hit: SC_ILLUSIONDOPING at <c>100 - 10*lv %</c>; on success also
/// SC_HALLUCINATION.
/// </summary>
public sealed class IllusionDoping : RecursiveDamageSplashSkillImpl
{
    private readonly Random _rng;

    public IllusionDoping() : base(SkillIds.GN_ILLUSIONDOPING) => _rng = Random.Shared;

    public IllusionDoping(Random? rng = null) : base(SkillIds.GN_ILLUSIONDOPING) => _rng = rng ?? Random.Shared;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 100 - skillLevel * 10)
        {
            ctx.Sc?.Start(target, StatusType.Illusiondoping, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
            ctx.Sc?.Start(target, StatusType.Hallucination, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        }
    }
}
