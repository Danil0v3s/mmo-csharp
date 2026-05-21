using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_STORMSLASH — Dragon Knight Storm Slash. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/stormslash.cpp</c>.
/// Ratio <c>+(-100 + 300 + 750*lv) + 5*POW</c>; ×2 at 60% chance under
/// SC_GIANTGROWTH. SC plumb into ratio is TODO.
/// </summary>
public sealed class StormSlash : WeaponSkillImpl
{
    public StormSlash() : base(SkillIds.DK_STORMSLASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 300 + 750 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
