using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_MAGMA_ERUPTION — Mechanic Magma Eruption. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/magmaeruption.cpp</c>.
/// Ratio: <c>+(350 + 50*lv)</c>. On-hit SC_STUN at 90 %. Splash +
/// follow-up unit TODO.
/// </summary>
public sealed class MagmaEruption : WeaponSkillImpl
{
    private readonly Random _rng;

    public MagmaEruption() : base(SkillIds.NC_MAGMA_ERUPTION) => _rng = Random.Shared;

    public MagmaEruption(Random? rng = null) : base(SkillIds.NC_MAGMA_ERUPTION) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 350 + 50 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 90)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5000, src);
    }
}
