using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MO_INVESTIGATE (id 266) — Monk Investigate (Occult Impaction).
/// rAthena <c>skill.cpp:case MO_INVESTIGATE</c>: DEF-ignoring physical
/// hit at (75 + 25 * lv)% ATK; damage scales with target's DEF (the
/// higher the DEF, the higher the damage — the "punisher" skill for
/// tanky targets).
///
/// First-port: defer to generic Weapon resolver and document that
/// the SimpleDefense bypass + DEF-scaling math wants its own resolver
/// hook (NK_SIMPLEDEFENSE in rAthena). Plugin currently runs vanilla
/// physical damage; the DEF-bypass tier ports here later.
/// </summary>
public sealed class InvestigateBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MO_INVESTIGATE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Approximate: per-hit damage scales with target's DEF.
        // rAthena: damage = ATK% * (target.def + target.def2) / 50.
        var rate = 75 + 25 * skillLevel;
        var swing = ctx.Battle.CalcWeaponAttack(source, target);
        var defFactor = (target.Stats.Def + target.Stats.Def2) / 50;
        if (defFactor < 1) defFactor = 1;
        var dmg = (int)Math.Clamp(swing.Total * rate / 100 * defFactor, 0, int.MaxValue);
        ctx.Damage.ApplyDamage(target, Math.Max(1, dmg), source);
        return true;
    }
}
