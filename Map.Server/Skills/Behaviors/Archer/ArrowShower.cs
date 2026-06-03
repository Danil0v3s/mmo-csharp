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
        // COMBAT-96 — skill-aware swing + the ÷200 skill crit_atk_rate bump (battle.cpp:7787).
        var swing = ctx.Battle.CalcWeaponAttack(src, victim, SkillId);
        var rate = 75 + 5 * skillLevel;
        return ApplySkillCritAtkRate((long)swing.Total * rate / 100, src, swing);
    }
}
