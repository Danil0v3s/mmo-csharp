using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_PRONTERA_RECALL — auto-generated stub from
/// <c>src/map/skills/other/pronterarecall.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class PronteraRecall : SkillImpl
{
    public PronteraRecall() : base(SkillIds.ALL_PRONTERA_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( sd != nullptr ){
    // 		// Destination position.
    // 		uint16 x;
    // 		uint16 y;
    // 
    // 		if(skill_lv == 1) {
    // 			x = 115;
    // 			y = 72;
    // 		}
    // 		else if(skill_lv == 2) {
    // 			x = 159;
    // 			y = 192;
    // 		}
    // 		uint16 mapindex  = mapindex_name2id(MAP_PRONTERA);
    // 
    // 		sc_start( src, target, type, 100, skill_lv, skill_get_cooldown( getSkillId(), skill_lv ) );
    // 
    // 		if(!mapindex)
    // 		{ //Given map not found?
    // 			clif_skill_fail( *sd, getSkillId() );
    // 			flag |= SKILL_NOCONSUME_REQ;
    // 			return;
    // 		}
    // 
    // 		pc_setpos(sd, mapindex, x, y, CLR_TELEPORT);
    // 	}
    }
}
