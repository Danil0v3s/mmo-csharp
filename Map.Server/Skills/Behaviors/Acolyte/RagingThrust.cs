using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_COMBOFINISH — auto-generated stub from
/// <c>src/map/skills/acolyte/ragingthrust.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class RagingThrust : WeaponSkillImpl
{
    public RagingThrust() : base(SkillIds.MO_COMBOFINISH) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change* sc = status_get_sc(src);
    // 
    // 	if (!(flag&1) && sc && sc->getSCE(SC_SPIRIT) && sc->getSCE(SC_SPIRIT)->val2 == SL_MONK)
    // 	{	//Becomes a splash attack when Soul Linked.
    // 		map_foreachinshootrange(skill_area_sub, target,
    // 			skill_get_splash(getSkillId(), skill_lv),BL_CHAR|BL_SKILL,
    // 			src,getSkillId(),skill_lv,tick, flag|BCT_ENEMY|1,
    // 			skill_castend_damage_id);
    // 	} else
    // 		WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	base_skillratio += 450 + 50 * skill_lv + sstatus->str; // !TODO: How does STR play a role?
    // #else
    // 	base_skillratio += 140 + 60 * skill_lv;
    // #endif
    // 
    // 	if (const status_change* sc = status_get_sc(src); sc != nullptr && sc->getSCE(SC_GT_ENERGYGAIN))
    // 		base_skillratio += base_skillratio * 50 / 100;
    return baseRatio;
    }
}
