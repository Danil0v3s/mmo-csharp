using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Blacksmith;

/// <summary>
/// BS_HAMMERFALL — Blacksmith Hammer Fall. Mirrors
/// <c>rathena-fork/src/map/skills/blacksmith/hammerfall.cpp</c>.
///
/// Standard weapon hit + stun chance (20 + 10*lv)%. Stun duration
/// 2000 + 200*lv ms. Damage flows through skill_db DamageRate
/// (no per-skill ratio bump).
/// </summary>
public sealed class HammerFall : WeaponSkillImpl
{
    private readonly Random _rng;

    public HammerFall(Random? rng = null) : base(SkillIds.BS_HAMMERFALL)
    {
        _rng = rng ?? Random.Shared;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var chance = 20 + 10 * skillLevel;
        if (_rng.Next(100) < chance)
        {
            var stunMs = 2_000 + 200 * skillLevel;
            ctx.Sc.Start(target, StatusType.Stun, val1: 1, 0, 0, 0, stunMs, src);
        }
    }
}
