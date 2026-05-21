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
        // (lv+1)/2 magic bolts; net damage is the same.
        var hitCount = (skillLevel + 1) / 2;
        var perHit = MagicBoltHelper.PerHitDamage(src);
        for (var hit = 0; hit < hitCount; hit++)
        {
            ctx.Damage.ApplyDamage(target, perHit, src);
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
