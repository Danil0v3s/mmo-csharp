using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_SHOWER — Archer Arrow Shower. Mirrors
/// <c>rathena-fork/src/map/skills/archer/arrowshower.cpp</c>.
///
/// Ground-targeted ranged AoE, 3×3 splash. Each victim takes
/// (75 + 5*lv)% ATK ranged damage.
/// </summary>
public sealed class ArrowShower : RecursiveDamageSplashSkillImpl
{
    public ArrowShower() : base(SkillIds.AC_SHOWER) { }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 1;

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, victim);
        var rate = 75 + 5 * skillLevel;
        return swing.Total * rate / 100;
    }
}
