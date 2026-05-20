using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ranger;

/// <summary>
/// RA_ARROWSTORM — Ranger Arrow Storm. Mirrors
/// <c>rathena-fork/src/map/skills/ranger/arrowstorm.cpp</c>.
///
/// Ground-target ranged splash (radius 4). Per-victim
/// (1300 + 200*lv)% ATK. Consumes one arrow.
/// </summary>
public sealed class ArrowStorm : RecursiveDamageSplashSkillImpl
{
    public ArrowStorm() : base(SkillIds.RA_ARROWSTORM) { }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 4;

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, victim);
        var rate = 1300 + 200 * skillLevel;
        return swing.Total * rate / 100;
    }
}
