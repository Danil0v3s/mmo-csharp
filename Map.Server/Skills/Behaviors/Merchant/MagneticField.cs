using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_MAGNETICFIELD — auto-generated stub from
/// <c>src/map/skills/merchant/magneticfield.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MagneticField : SkillImpl
{
    public MagneticField() : base(SkillIds.NC_MAGNETICFIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag & 1) {
    // 		sc_start2(src, target, SC_MAGNETICFIELD, 100, skill_lv, src->id, skill_get_time(getSkillId(), skill_lv));
    // 	} else {
    // 		if (map_flag_vs(src->m)) // Doesn't affect the caster in non-PVP maps [exneval]
    // 			sc_start2(src, target, skill_get_sc(getSkillId()), 100, skill_lv, src->id, skill_get_time(getSkillId(), skill_lv));
    // 		map_foreachinallrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), splash_target(src), src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_nodamage_id);
    // 		clif_skill_damage( *src, *target, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // 	}
    }
}
