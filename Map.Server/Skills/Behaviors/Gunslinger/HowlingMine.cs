using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_H_MINE — auto-generated stub from
/// <c>src/map/skills/gunslinger/howlingmine.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HowlingMine : SkillImpl
{
    public HowlingMine() : base(SkillIds.RL_H_MINE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	status_change* tsc = status_get_sc(target);
    // 
    // 	if (!(flag & 1)) {
    // 		// Direct attack
    // 		if (!sd || !sd->flicker) {
    // 			if (skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag)) {
    // 				status_change_start(src, target, SC_H_MINE, 10000, getSkillId(), 0, 0, 0, skill_get_time(getSkillId(), skill_lv), SCSTART_NOAVOID | SCSTART_NOTICKDEF | SCSTART_NORATEDEF);
    // 			}
    // 			return;
    // 		}
    // 
    // 		// Triggered by RL_FLICKER
    // 		map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR | BL_SKILL,
    // 			src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | 1, skill_castend_damage_id);
    // 		flag |= 1; // Don't consume requirement
    // 
    // 		if (tsc && tsc->getSCE(SC_H_MINE) && tsc->getSCE(SC_H_MINE)->val2 == src->id) {
    // 			status_change_end(target, SC_H_MINE);
    // 			sc_start4(src, target, SC_BURNING, 10 * skill_lv, skill_lv, 1000, src->id, 0, skill_get_time2(getSkillId(), skill_lv));
    // 		}
    // 	} else {
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 	}
    // 
    // 	if (sd && sd->flicker) {
    // 		flag |= 1; // Don't consume requirement
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd && sd->flicker) {
    // 		// Flicker explosion damage: 500 + 300 * SkillLv
    // 		skillratio += -100 + 500 + 300 * skill_lv;
    // 	} else {
    // 		// Direct trigger damage: 200 + 200 * SkillLv
    // 		skillratio += -100 + 200 + 200 * skill_lv;
    // 	}
    return baseRatio;
    }
}
