using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_WARP — auto-generated stub from
/// <c>src/map/skills/acolyte/warpportal.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WarpPortal : SkillImpl
{
    public WarpPortal() : base(SkillIds.AL_WARP) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	status_change* sc = status_get_sc(src);
    // 
    // 	if(sd != nullptr) {
    // 		std::vector<std::string> maps( MAX_MEMOPOINTS + 1 );
    // 
    // 		maps.push_back( sd->status.save_point.map );
    // 
    // 		if( skill_lv >= 2 ){
    // 			maps.push_back( sd->status.memo_point[0].map );
    // 
    // 			if( skill_lv >= 3 ){
    // 				maps.push_back( sd->status.memo_point[1].map );
    // 
    // 				if( skill_lv >= 4 ){
    // 					maps.push_back( sd->status.memo_point[2].map );
    // 				}
    // 			}
    // 		}
    // 
    // 		clif_skill_warppoint( *sd, getSkillId(), skill_lv, maps );
    // 	}
    // 	if( sc && sc->getSCE(SC_CURSEDCIRCLE_ATKER) ) //Should only remove after the skill has been casted.
    // 		status_change_end(src,SC_CURSEDCIRCLE_ATKER);
    // 	// not to consume item.
    // 	flag |= SKILL_NOCONSUME_REQ;
    }
}
