using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// HVAN_EXPLOSION — auto-generated stub from
/// <c>src/map/skills/homunculus/homunculus_bioexplosion.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class BioExplosion : SkillImpl
{
    public BioExplosion() : base(SkillIds.HVAN_EXPLOSION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // homun_data* hd = BL_CAST(BL_HOM, src);
    // 
    // 	if (hd != nullptr) {
    // 		clif_skill_nodamage(src, *src, getSkillId(), skill_lv, 1);
    // 		map_foreachinshootrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR | BL_SKILL, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY, skill_castend_damage_id);
    // 
    // 		hd->homunculus.intimacy = hom_intimacy_grade2intimacy(HOMGRADE_HATE_WITH_PASSION);
    // 		clif_send_homdata(*hd, SP_INTIMATE);
    // 
    // 		// There's a delay between the explosion and the homunculus death
    // 		skill_addtimerskill(src, tick + skill_get_time(getSkillId(), skill_lv), src->id, 0, 0, getSkillId(), skill_lv, 0, flag);
    // 	}
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (src != target) {
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 	}
    }
}
