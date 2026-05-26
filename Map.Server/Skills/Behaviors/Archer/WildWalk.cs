using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_WILD_WALK — Wind Hawk Wild Walk. Manual port of
/// <c>rathena-fork/src/map/skills/archer/wildwalk.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 1800 + 2800*lv) + 5*CON</c>; running ratio
/// is then scaled by WH_NATUREFRIENDLY/10 + HT_STEELCROW/10. The hit
/// is a normal weapon attack; SC_WILD_WALK is then applied on the
/// caster.</para>
/// </summary>
public sealed class WildWalk : WeaponSkillImpl
{
    public WildWalk() : base(SkillIds.WH_WILD_WALK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 1800 + 2800 * skillLevel) + 5 * src.Stats.Con;
        if (src is PlayerEntity pc)
        {
            var nature = ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WH_NATUREFRIENDLY) ?? 0;
            var steel = ctx.PlayerSkill?.CheckSkill(pc, SkillIds.HT_STEELCROW) ?? 0;
            ratio += ratio * nature / 10;
            ratio += ratio * steel / 10;
        }
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        base.CastendDamageId(src, target, skillLevel, ctx);
        ctx.Sc?.Start(src, StatusType.WildWalk, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
