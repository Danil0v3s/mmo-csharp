using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_EARTHSHAKER — auto-generated stub from
/// <c>src/map/skills/acolyte/earthshaker.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class EarthShaker : WeaponSkillImpl
{
    public EarthShaker() : base(SkillIds.SR_EARTHSHAKER) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 
    // 	if (dstmd != nullptr && dstmd->guardian_data == nullptr) // Target is a mob (boss included) and not a guardian type. [Atemo]
    // 		sc_start(src, target, SC_EARTHSHAKER, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // 	sc_start(src,target,SC_STUN, 25 + 5 * skill_lv,skill_lv,skill_get_time(getSkillId(),skill_lv));
    // 	status_change_end(target, SC_SV_ROOTTWIST);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *tsc = status_get_sc(target);
    // 
    // 	if (tsc && ((tsc->option&(OPTION_HIDE|OPTION_CLOAK|OPTION_CHASEWALK)) || tsc->getSCE(SC_CAMOUFLAGE) || tsc->getSCE(SC_STEALTHFIELD) || tsc->getSCE(SC__SHADOWFORM))) {
    // 		//[(Skill Level x 300) x (Caster Base Level / 100) + (Caster STR x 3)] %
    // 		skillratio += -100 + 300 * skill_lv;
    // 		RE_LVL_DMOD(100);
    // 		skillratio += status_get_str(src) * 3;
    // 	} else { //[(Skill Level x 400) x (Caster Base Level / 100) + (Caster STR x 2)] %
    // 		skillratio += -100 + 400 * skill_lv;
    // 		RE_LVL_DMOD(100);
    // 		skillratio += status_get_str(src) * 2;
    // 	}
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 
    // 	if( flag&1 ) { //by default cloaking skills are remove by aoe skills so no more checking/removing except hiding and cloaking exceed.
    // 		WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 		status_change_end(target, SC_CLOAKINGEXCEED);
    // 		if (tsc && tsc->getSCE(SC__SHADOWFORM) && rnd() % 100 < 100 - tsc->getSCE(SC__SHADOWFORM)->val1 * 10) // [100 - (Skill Level x 10)] %
    // 			status_change_end(target, SC__SHADOWFORM);
    // 	} else {
    // 		map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR|BL_SKILL, src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|SD_SPLASH|1, skill_castend_damage_id);
    // 		clif_skill_damage( *src, *src, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // 	}
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_castend_damage_id(src, src, getSkillId(), skill_lv, tick, flag);
    }
}
