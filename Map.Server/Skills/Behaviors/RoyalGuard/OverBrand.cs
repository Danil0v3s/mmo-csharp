using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.RoyalGuard;

/// <summary>
/// LG_OVERBRAND — Royal Guard Over Brand. Mirrors
/// <c>rathena-fork/src/map/skills/royalguard/overbrand.cpp</c>.
///
/// Line-shaped splash from caster forward 3 cells. Per-victim
/// (400 + 50*lv)% ATK. Requires spear weapon.
///
/// First-port: 3-cell radius around the target.
/// </summary>
public sealed class OverBrand : RecursiveDamageSplashSkillImpl
{
    public OverBrand() : base(SkillIds.LG_OVERBRAND) { }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 3;

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, victim);
        var rate = 400 + 50 * skillLevel;
        return swing.Total * rate / 100;
    }
}
