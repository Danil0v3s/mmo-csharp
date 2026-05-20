using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_DOUBLE — Archer Double Strafe. Mirrors
/// <c>rathena-fork/src/map/skills/archer/doublestrafe.cpp</c>.
///
/// 2-hit ranged physical, each at (90 + 10*lv)% ATK.
/// </summary>
public sealed class DoubleStrafe : SkillImpl
{
    public DoubleStrafe() : base(SkillIds.AC_DOUBLE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 90 + 10 * skillLevel;
        for (var hit = 0; hit < 2; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(src, target);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, src);
        }
    }
}
