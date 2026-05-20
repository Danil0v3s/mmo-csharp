using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_GUARDIAN_RECALL — auto-generated stub from
/// <c>src/map/skills/other/guardiansrecall.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class GuardiansRecall : SkillImpl
{
    public GuardiansRecall() : base(SkillIds.ALL_GUARDIAN_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( sd != nullptr ){
    // 		// Destination position.
    // 		uint16 x = 44;
    // 		uint16 y = 151;
    // 		uint16 mapindex  = mapindex_name2id(MAP_MORA);
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
