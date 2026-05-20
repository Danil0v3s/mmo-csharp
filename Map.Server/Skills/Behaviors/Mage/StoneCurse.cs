using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_STONECURSE — Mage Stone Curse. Mirrors
/// <c>rathena-fork/src/map/skills/mage/stonecurse.cpp</c>.
///
/// Single-target Earth magic + petrify chance (24 + 2*lv)%. The
/// petrify flows through SC_STONEWAIT (5 s warmup) then SC_STONE.
/// </summary>
public sealed class StoneCurse : SkillImpl
{
    private const int StoneWaitMs = 5_000;
    private readonly Random _rng;

    public StoneCurse(Random? rng = null) : base(SkillIds.MG_STONECURSE)
    {
        _rng = rng ?? Random.Shared;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var perHit = MagicBoltHelper.PerHitDamage(src);
        ctx.Damage.ApplyDamage(target, perHit, src);
        ApplyAdditionalEffects(src, target, skillLevel, ctx);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var chance = 24 + 2 * skillLevel;
        if (_rng.Next(100) < chance)
        {
            ctx.Sc.Start(target, StatusType.Stonewait, val1: 1, 0, 0, 0, StoneWaitMs, src);
        }
    }
}
