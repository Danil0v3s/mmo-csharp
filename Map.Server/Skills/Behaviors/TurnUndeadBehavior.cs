using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// PR_TURNUNDEAD (id 77) — Priest Turn Undead. rAthena
/// <c>skill.cpp:case PR_TURNUNDEAD</c>: Holy magic vs Undead target
/// only; refuses to apply against living targets. Damage rolls a
/// "send to grave" instakill chance vs Undead with formula
/// <c>20 + caster_lv + skill_lv + (luk/10) - target_lv</c>%. On miss,
/// applies 100 % MATK Holy damage.
///
/// For non-Undead targets the cast no-ops (rAthena: cast still
/// completes but does 0 damage). We honor the "target must be Undead"
/// short-circuit and run normal Holy damage when valid.
/// </summary>
public sealed class TurnUndeadBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.PR_TURNUNDEAD;

    private readonly Random _rng;
    public TurnUndeadBehavior(Random? rng = null) { _rng = rng ?? Random.Shared; }

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Element-undead test (renewal: also race-undead, but for the
        // first port the element check is the canonical filter).
        if (target.Stats.DefenseElement != BattleElement.Undead) return true;

        // Instakill chance.
        var chance = 20 + source.Level + skillLevel + source.Stats.Luk / 10 - target.Level;
        chance = Math.Clamp(chance, 0, 100);
        if (_rng.Next(100) < chance)
        {
            // Effective instakill: set HP to 1 then let the standard
            // damage path finish the job through the usual death pipe.
            var hp = target switch
            {
                PlayerEntity p => p.Hp,
                MobEntity m => m.Hp,
                _ => 0,
            };
            if (hp > 0) ctx.Damage.ApplyDamage(target, hp, source);
            return true;
        }

        // Fallback Holy damage — defer to Magic resolver so skill_db's
        // DamageRate + element fix apply consistently.
        return false;
    }
}
