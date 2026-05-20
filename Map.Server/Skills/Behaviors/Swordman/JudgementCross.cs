using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_JUDGEMENT_CROSS — auto-generated stub from
/// <c>src/map/skills/swordman/judgementcross.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class JudgementCross : SkillImpl
{
    public JudgementCross() : base(SkillIds.IG_JUDGEMENT_CROSS) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 	int32 i;
    // 
    // 	skillratio += -100 + 1950 * skill_lv + 10 * sstatus->spl;
    // 	if (tstatus->race == RC_PLANT || tstatus->race == RC_INSECT)
    // 		skillratio += 150 * skill_lv;
    // 	RE_LVL_DMOD(100);
    // 	if ((i = pc_checkskill_imperial_guard(sd, 3)) > 0)
    // 		skillratio += skillratio * i / 100;
    return baseRatio;
    }
}
