using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_SHIELDCHARGE — Crusader Shield Charge / Smite. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/smite.cpp</c>.
/// Ratio <c>+20*lv</c>. <c>15 + 5*lv</c>% chance to stun on hit.
/// </summary>
public sealed class Smite : WeaponSkillImpl
{
    private readonly Random _rng;

    public Smite() : base(SkillIds.CR_SHIELDCHARGE) => _rng = Random.Shared;

    public Smite(Random? rng = null) : base(SkillIds.CR_SHIELDCHARGE)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 20 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 15 + 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
