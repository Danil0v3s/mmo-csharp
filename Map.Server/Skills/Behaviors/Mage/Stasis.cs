using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_STASIS — auto-generated stub from
/// <c>src/map/skills/mage/stasis.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Stasis : SkillImpl
{
    public Stasis() : base(SkillIds.WL_STASIS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 
    // 	if (flag&1)
    // 		sc_start(src,target,type,100,skill_lv,skill_get_time(getSkillId(),skill_lv));
    // 	else {
    // 		struct map_data *mapdata = map_getmapdata(src->m);
    // 
    // 		map_foreachinallrange(skill_area_sub,src,skill_get_splash(getSkillId(), skill_lv),BL_CHAR,src,getSkillId(),skill_lv,tick,(mapdata_flag_vs(mapdata)?BCT_ALL:BCT_ENEMY|BCT_SELF)|flag|1,skill_castend_nodamage_id);
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	}
    }
}
