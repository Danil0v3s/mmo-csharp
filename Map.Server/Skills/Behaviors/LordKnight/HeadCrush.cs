using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.LordKnight;

/// <summary>
/// LK_HEADCRUSH — Lord Knight Head Crush. Mirrors
/// <c>rathena-fork/src/map/skills/lordknight/headcrush.cpp</c>.
///
/// Standard weapon hit at (100 + 30*lv)% ATK + bleeding proc at
/// (40 + 5*lv)% chance. Damage flows through WeaponSkillImpl;
/// plugin overrides ratio + the bleeding side-effect.
/// </summary>
public sealed class HeadCrush : WeaponSkillImpl
{
    private readonly Random _rng;

    public HeadCrush(Random? rng = null) : base(SkillIds.LK_HEADCRUSH)
    {
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 30 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var chance = 40 + 5 * skillLevel;
        if (_rng.Next(100) < chance)
        {
            // Bleeding ticks MaxHp/100 every 10 s for 60 s.
            ctx.Sc.Start(target, StatusType.Bleeding, val1: 1, 0, 0, 0,
                durationMs: 60_000, src);
        }
    }
}
