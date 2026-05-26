using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_STORMSLASH — Dragon Knight Storm Slash. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/stormslash.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 300 + 750*lv) + 5*POW</c>; doubles at 60 %
/// chance when <see cref="StatusType.Giantgrowth"/> is active.</para>
/// </summary>
public sealed class StormSlash : WeaponSkillImpl
{
    private readonly Random _rng;

    public StormSlash() : base(SkillIds.DK_STORMSLASH) => _rng = Random.Shared;

    public StormSlash(Random? rng = null) : base(SkillIds.DK_STORMSLASH)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 300 + 750 * skillLevel) + 5 * src.Stats.Pow;
        if (ctx.Sc?.Get(src, StatusType.Giantgrowth) != null && _rng.Next(100) < 60)
            ratio *= 2;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
