using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_FROSTDIVER — Mage Frost Diver. Mirrors
/// <c>rathena-fork/src/map/skills/mage/frostdiver.cpp</c>.
///
/// Single-target Water magic + freeze chance (30 + 3*lv)%. Defers
/// damage to the generic Magic resolver via the standard
/// <see cref="WeaponSkillImpl"/>-style pipeline; this class
/// overrides only <see cref="ApplyAdditionalEffects"/> for the
/// freeze proc.
/// </summary>
public sealed class FrostDiver : SkillImpl
{
    private const int FreezeMs = 3_000;
    private readonly Random _rng;

    public FrostDiver(Random? rng = null) : base(SkillIds.MG_FROSTDIVER)
    {
        _rng = rng ?? Random.Shared;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Damage path = standard magic bolt midpoint.
        var perHit = MagicBoltHelper.PerHitDamage(src);
        ctx.Damage.ApplyDamage(target, perHit, src);
        ApplyAdditionalEffects(src, target, skillLevel, ctx);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var chance = 30 + 3 * skillLevel;
        if (_rng.Next(100) < chance)
        {
            ctx.Sc.Start(target, StatusType.Freeze, val1: 1, 0, 0, 0, FreezeMs, src);
        }
    }
}
