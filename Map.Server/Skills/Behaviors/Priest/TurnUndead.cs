using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Priest;

/// <summary>
/// PR_TURNUNDEAD — Priest Turn Undead. Mirrors
/// <c>rathena-fork/src/map/skills/priest/turnundead.cpp</c>.
///
/// Holy magic vs Undead-element only. Rolls
/// <c>20 + caster_lv + skill_lv + luk/10 - target_lv</c>%
/// instakill chance; on miss deals 100 % MATK Holy. Non-Undead
/// targets no-op (rAthena: cast completes but does 0 damage).
/// </summary>
public sealed class TurnUndead : SkillImpl
{
    private readonly Random _rng;

    public TurnUndead(Random? rng = null) : base(SkillIds.PR_TURNUNDEAD)
    {
        _rng = rng ?? Random.Shared;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target.Stats.DefenseElement != BattleElement.Undead) return;

        var chance = 20 + src.Level + skillLevel + src.Stats.Luk / 10 - target.Level;
        chance = Math.Clamp(chance, 0, 100);
        if (_rng.Next(100) < chance)
        {
            var hp = target switch
            {
                PlayerEntity p => p.Hp,
                MobEntity m => m.Hp,
                _ => 0,
            };
            if (hp > 0) ctx.Damage.ApplyDamage(target, hp, src);
            return;
        }

        // Fallback Holy damage on miss.
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        ctx.Damage.ApplyDamage(target, matk, src);
    }
}
