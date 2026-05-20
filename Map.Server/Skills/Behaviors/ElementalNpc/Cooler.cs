using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_COOLER — auto-generated stub from
/// <c>src/map/skills/elemental/cooler.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Cooler : SkillImpl
{
    public Cooler() : base(SkillIds.EL_COOLER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	s_elemental_data *ele = BL_CAST(BL_ELEM, src);
    // 	if( ele ) {
    // 		sc_type type2 = (sc_type)(type-1);
    // 		status_change *esc = status_get_sc(ele);
    // 
    // 		if( (esc && esc->getSCE(type2)) || (tsc && tsc->getSCE(type)) ) {
    // 			status_change_end(src,type);
    // 			status_change_end(target,type2);
    // 		} else {
    // 			clif_skill_nodamage(src,*src,getSkillId(),skill_lv);
    // 			clif_skill_damage( *src, *target, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // 			sc_start(src,src,type2,100,skill_lv,skill_get_time(getSkillId(),skill_lv));
    // 			sc_start(src,target,type,100,skill_lv,skill_get_time(getSkillId(),skill_lv));
    // 		}
    // 	}
    }
}
