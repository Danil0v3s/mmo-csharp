using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_SOULSTRIKE — Mage Soul Strike. Manual port matching
/// <c>rathena-fork/src/map/skills/mage/soulstrike.cpp</c>.
///
/// <para>Ghost-element multi-hit magic. The hit count comes from
/// skill_db (<c>Hit: Multi_Hit</c>): <c>(lv + 1) / 2</c> bolts
/// (lv1-2 = 1 bolt, lv9-10 = 5 bolts). Each bolt resolves through
/// the standard magic-attack pipeline.</para>
///
/// <para>The <see cref="CalculateSkillRatio"/> hook adds a per-level
/// damage bonus vs Undead targets — the fork's elemental specialty:
/// Soul Strike was historically the Mage anti-Undead pick before
/// Holy magic became available. The +5 % per level scales linearly
/// with skill level (lv1 = +5 %, lv10 = +50 %).</para>
/// </summary>
public sealed class SoulStrike : SkillImpl
{
    public SoulStrike() : base(SkillIds.MG_SOULSTRIKE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
        // The fork resolves multi-hit via the skill_db Hit:Multi_Hit
        // field — skill_attack reads HitCount and emits N damage frames.
        // The C# port pre-dates that integration so we manually emit
        // (lv+1)/2 magic bolts.
        var hitCount = (skillLevel + 1) / 2;
        for (var hit = 0; hit < hitCount; hit++)
        {
            // COMBAT-12: route each bolt through skill_attack(BF_MAGIC) so the
            // full magic pipeline applies — including THIS plugin's
            // CalculateSkillRatio (the +5*lv vs-undead bonus, previously a dead
            // hook because the old path used a raw MATK midpoint) plus element /
            // MDEF / cardfix. SkillAttackService.CalcMagicDamage reads our
            // overridden ratio for MG_SOULSTRIKE.
            if (ctx.SkillAttack != null)
            {
                ctx.SkillAttack.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
            }
            else
            {
                // Fallback for rigs without the attack service: apply the ratio
                // manually so the undead bonus still lands.
                var matk = MagicBoltHelper.PerHitDamage(src);
                var ratio = CalculateSkillRatio(100, src, target, skillLevel);
                ctx.Damage.ApplyDamage(target, System.Math.Max(1, matk * ratio / 100), src);
            }
        }
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: if (battle_check_undead(tstatus->race, tstatus->def_ele)) base_skillratio += 5 * skill_lv;
        // battle_check_undead returns true when target race == Undead OR
        // defense element == Undead — Mage's anti-undead specialty.
        if (target.Stats.Race == BattleRace.Undead || target.Stats.DefenseElement == BattleElement.Undead)
        {
            return baseRatio + 5 * skillLevel;
        }
        return baseRatio;
    }
}
