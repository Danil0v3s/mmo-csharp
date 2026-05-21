using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_MASS_SPIRAL — Rebellion Mass Spiral. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/massspiral.cpp</c>.
/// Ratio <c>+(-100 + 200*lv)</c>. (30 + 10*lv)% bleed on hit.
/// </summary>
public sealed class MassSpiral : WeaponSkillImpl
{
    private readonly Random _rng;

    public MassSpiral() : base(SkillIds.RL_MASS_SPIRAL) => _rng = Random.Shared;

    public MassSpiral(Random? rng = null) : base(SkillIds.RL_MASS_SPIRAL)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 30 + 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
    }
}
