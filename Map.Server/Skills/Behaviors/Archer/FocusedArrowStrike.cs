using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// SN_SHARPSHOOTING — auto-generated stub from
/// <c>src/map/skills/archer/focusedarrowstrike.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FocusedArrowStrike : RecursiveDamageSplashSkillImpl
{
    public FocusedArrowStrike() : base(SkillIds.SN_SHARPSHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (src->type == BL_MOB) { // TODO: Did these formulas change in the renewal balancing?
    // 		if (wd->miscflag & 2) // Splash damage bonus
    // 			skillratio += -100 + 140 * skill_lv;
    // 		else
    // 			skillratio += 100 + 50 * skill_lv;
    // 		return;
    // 	}
    // #ifdef RENEWAL
    // 	skillratio += -100 + 300 + 300 * skill_lv;
    // 	RE_LVL_DMOD(100);
    // #else
    // 	skillratio += 100 + 50 * skill_lv;
    // #endif
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	SkillImplRecursiveDamageSplash::castendDamageId(src, target, skill_lv, tick, flag);
    // 
    // 	if( flag&1 ) {
    // 		status_change_end(src, SC_CAMOUFLAGE);
    // 	}
    // #else
    // 	flag |= 2; // Flag for specific mob damage formula
    // 	skill_area_temp[1] = target->id;
    // 	if (battle_config.skill_eightpath_algorithm) {
    // 		//Use official AoE algorithm
    // 		if (!(map_foreachindir(skill_attack_area, src->m, src->x, src->y, target->x, target->y,
    // 		   skill_get_splash(getSkillId(), skill_lv), skill_get_maxcount(getSkillId(), skill_lv), 0, splash_target(src),
    // 		   skill_get_type(getSkillId()), src, src, getSkillId(), skill_lv, tick, flag, BCT_ENEMY))) {
    // 			flag &= ~2; // Only targets in the splash area are affected
    // 
    // 			//These skills hit at least the target if the AoE doesn't hit
    // 			skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 		}
    // 	} else {
    // 		map_foreachinpath(skill_attack_area, src->m, src->x, src->y, target->x, target->y,
    // 			skill_get_splash(getSkillId(), skill_lv), skill_get_maxcount(getSkillId(), skill_lv), splash_target(src),
    // 			skill_get_type(getSkillId()), src, src, getSkillId(), skill_lv, tick, flag, BCT_ENEMY);
    // 	}
    // #endif
    }
}
