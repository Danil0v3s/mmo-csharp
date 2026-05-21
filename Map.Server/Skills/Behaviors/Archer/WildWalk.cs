using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_WILD_WALK — Wind Hawk Wild Walk. Manual port of
/// <c>rathena-fork/src/map/skills/archer/wildwalk.cpp</c>.
/// Ratio <c>+(-100 + 1800 + 2800*lv) + 5*CON</c>. Self-buffs SC_WILD_WALK
/// after damage. WH_NATUREFRIENDLY / HT_STEELCROW passive scales TODO.
/// </summary>
public sealed class WildWalk : WeaponSkillImpl
{
    public WildWalk() : base(SkillIds.WH_WILD_WALK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1800 + 2800 * skillLevel) + 5 * src.Stats.Con;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        base.CastendDamageId(src, target, skillLevel, ctx);
        ctx.Sc?.Start(src, StatusType.WildWalk, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
