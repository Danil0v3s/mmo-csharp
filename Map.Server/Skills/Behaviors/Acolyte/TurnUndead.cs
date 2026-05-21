using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_TURNUNDEAD — Priest Turn Undead. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/turnundead.cpp</c>.
///
/// <para>Holy-element magic attack that only resolves when the
/// target is Undead-race or Undead-element. Non-undead targets
/// silently fail (no broadcast, no damage). The actual instant-
/// kill chance is computed inside <c>battle_calc_attack</c>'s
/// PR_TURNUNDEAD branch — this entry point just gates and routes.</para>
/// </summary>
public sealed class TurnUndead : SkillImpl
{
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    public TurnUndead() : base(SkillIds.PR_TURNUNDEAD) { }

    public TurnUndead(Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.PR_TURNUNDEAD)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: if (!battle_check_undead(tstatus->race, tstatus->def_ele)) return;
        if (target.Stats.Race != BattleRace.Undead && target.Stats.DefenseElement != BattleElement.Undead)
            return;

        // rAthena: skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
