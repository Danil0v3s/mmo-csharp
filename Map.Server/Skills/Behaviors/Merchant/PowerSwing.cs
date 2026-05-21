using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_POWERSWING — Mechanic Power Swing. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/powerswing.cpp</c>.
/// Ratio: <c>+(-100 + (STR+DEX)/2 + 300 + 100*lv)</c>. On-hit SC_STUN
/// at 10 %.
/// </summary>
public sealed class PowerSwing : WeaponSkillImpl
{
    private readonly Random _rng;

    public PowerSwing() : base(SkillIds.NC_POWERSWING) => _rng = Random.Shared;

    public PowerSwing(Random? rng = null) : base(SkillIds.NC_POWERSWING) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + ((src.Stats.Str + src.Stats.Dex) / 2) + 300 + 100 * skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 10)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 3000, src);
    }
}
