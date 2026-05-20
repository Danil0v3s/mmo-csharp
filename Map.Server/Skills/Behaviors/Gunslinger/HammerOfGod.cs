using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_HAMMER_OF_GOD — auto-generated stub from
/// <c>src/map/skills/gunslinger/hammerofgod.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HammerOfGod : RecursiveDamageSplashSkillImpl
{
    public HammerOfGod() : base(SkillIds.RL_HAMMER_OF_GOD) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* tsc = status_get_sc(target);
    // 
    // 	if (flag & 1) {
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag | SD_ANIMATION);
    // 		return;
    // 	}
    // 
    // 	if (sd && tsc && tsc->getSCE(SC_C_MARKER)) {
    // 		int32 i = 0;
    // 
    // 		ARR_FIND(0, MAX_SKILL_CRIMSON_MARKER, i, sd->c_marker[i] == target->id);
    // 		if (i < MAX_SKILL_CRIMSON_MARKER) {
    // 			flag |= 8;
    // 		}
    // 	}
    // 
    // 	clif_skill_poseffect(*src, getSkillId(), 1, target->x, target->y, gettick());
    // 	map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skillratio += -100 + 100 * skill_lv;
    // 	if (sd) {
    // 		if (wd->miscflag & 8) {
    // 			skillratio += 400 * sd->spiritball_old;
    // 		} else {
    // 			skillratio += 150 * sd->spiritball_old;
    // 		}
    // 	}
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
