using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_SCAROFTAROU — Summoner Scar of Tarou. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/scaroftarou.cpp</c>.
/// Ratio <c>+(-100 + 100*lv)</c>. 10% chance to stun + applies
/// SC_BITESCAR on hit. SU_SPIRITOFLIFE HP-ratio bonus is TODO.
/// </summary>
public sealed class ScarofTarou : WeaponSkillImpl
{
    private readonly Random _rng;

    public ScarofTarou() : base(SkillIds.SU_SCAROFTAROU) => _rng = Random.Shared;

    public ScarofTarou(Random? rng = null) : base(SkillIds.SU_SCAROFTAROU)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 100 * skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 10)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        if (_rng.Next(100) < 10)
            ctx.Sc?.Start(target, StatusType.Bitescar, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
