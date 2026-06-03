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
            // COMBAT-96 — skill-aware swing + the ÷200 skill crit_atk_rate bump (battle.cpp:7787).
            var swing = ctx.Battle.CalcWeaponAttack(src, target, SkillId);
            var raw = ApplySkillCritAtkRate((long)swing.Total * rate / 100, src, swing);
            ctx.Damage.ApplyDamage(target, (int)Math.Clamp(raw, 0, int.MaxValue), src);
        }
    }
}
