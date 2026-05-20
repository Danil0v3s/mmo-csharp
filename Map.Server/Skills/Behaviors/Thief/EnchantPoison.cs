using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_ENCHANTPOISON — auto-generated stub from
/// <c>src/map/skills/thief/enchantpoison.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class EnchantPoison : SkillImpl
{
    public EnchantPoison() : base(SkillIds.AS_ENCHANTPOISON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if( sc_start( src, target, type, 100, skill_lv, skill_get_time( getSkillId(), skill_lv ) ) ){
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	}else{
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv, false );
    // 
    // 		if( sd != nullptr ){
    // 			clif_skill_fail( *sd, getSkillId() );
    // 		}
    // 	}
    }
}
