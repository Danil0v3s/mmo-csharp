using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_ODINS_RECALL — auto-generated stub from
/// <c>src/map/skills/other/odinsrecall.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class OdinsRecall : SkillImpl
{
    public OdinsRecall() : base(SkillIds.ALL_ODINS_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if(sd != nullptr)
    // 	{
    // 		if (map_getmapflag(target->m, MF_NOTELEPORT) && skill_lv <= 2) {
    // 			clif_skill_teleportmessage( *sd, NOTIFY_MAPINFO_CANT_TP );
    // 			return;
    // 		}
    // 		if(!battle_config.duel_allow_teleport && sd->duel_group && skill_lv <= 2) { // duel restriction [LuzZza]
    // 			char output[128]; sprintf(output, msg_txt(sd,365), skill_get_name(ALL_ODINS_RECALL));
    // 			clif_displaymessage(sd->fd, output); //"Duel: Can't use %s in duel."
    // 			return;
    // 		}
    // 
    // 		if( sd->state.autocast || ( (sd->skillitem == AL_TELEPORT || battle_config.skip_teleport_lv1_menu) && skill_lv == 1 ) || skill_lv == 3 )
    // 		{
    // 			if( skill_lv == 1 )
    // 				pc_randomwarp(sd,CLR_TELEPORT);
    // 			else
    // 				pc_setpos( sd, mapindex_name2id( sd->status.save_point.map ), sd->status.save_point.x, sd->status.save_point.y, CLR_TELEPORT );
    // 			return;
    // 		}
    // 
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 
    // 		std::vector<std::string> maps = {
    // 			"Random"
    // 		};
    // 
    // 		maps.push_back( sd->status.save_point.map );
    // 
    // 		clif_skill_warppoint( *sd, getSkillId(), skill_lv, maps );
    // 	} else
    // 		unit_warp(target,-1,-1,-1,CLR_TELEPORT);
    }
}
