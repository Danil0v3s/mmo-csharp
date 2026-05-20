using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_SR_CURSEDCIRCLE — auto-generated stub from
/// <c>src/map/skills/npc/npccursedcircle.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NpcCursedCircle : SkillImpl
{
    public NpcCursedCircle() : base(SkillIds.NPC_SR_CURSEDCIRCLE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if( flag&1 ) {
    // 		if( status_get_class_(target) == CLASS_BOSS )
    // 			return;
    // 		if( sc_start2(src,target, skill_get_sc(getSkillId()), 50, skill_lv, src->id, skill_get_time(getSkillId(), skill_lv))) {
    // 			if( target->type == BL_MOB )
    // 				mob_unlocktarget((TBL_MOB*)target,gettick());
    // 			clif_bladestop( *src, target->id, true );
    // 			flag |= SKILL_NOCONSUME_REQ;
    // 			return;
    // 		}
    // 	} else {
    // 		map_session_data* sd = BL_CAST(BL_PC, src);
    // 		int32 count = 0;
    // 
    // 		clif_skill_damage( *src, *target, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // 		count = map_forcountinrange(skill_area_sub, src, skill_get_splash(getSkillId(),skill_lv), (sd)?sd->spiritball_old:15, // Assume 15 spiritballs in non-charactors
    // 			BL_CHAR, src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|1, skill_castend_nodamage_id);
    // 		if( sd ) pc_delspiritball(sd, count, 0);
    // 		clif_skill_nodamage(src, *src, getSkillId(), skill_lv,
    // 			sc_start2(src,src, SC_CURSEDCIRCLE_ATKER, 50, skill_lv, count, skill_get_time(getSkillId(),skill_lv)));
    // 	}
    }
}
