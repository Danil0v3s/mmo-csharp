using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_MEGA_SONIC_BLOW — Hyper Novice Mega Sonic Blow. Manual port of
/// <c>rathena-fork/src/map/skills/novice/megasonicblow.cpp</c>.
/// Ratio <c>+(-100 + 900 + 750*lv) + 5*POW</c>; doubles when target
/// HP &lt; MaxHP/2. (2*lv + 10)% chance to stun. HN_SELFSTUDY_TATICS
/// bonus is TODO.
/// </summary>
public sealed class MegaSonicBlow : WeaponSkillImpl
{
    private readonly Random _rng;

    public MegaSonicBlow() : base(SkillIds.HN_MEGA_SONIC_BLOW) => _rng = Random.Shared;

    public MegaSonicBlow(Random? rng = null) : base(SkillIds.HN_MEGA_SONIC_BLOW)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 900 + 750 * skillLevel) + 5 * src.Stats.Pow;
        if (target.Stats.Hp < target.Stats.MaxHp / 2)
            ratio *= 2;
        return ratio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 2 * skillLevel + 10)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
