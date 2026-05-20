using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_SHADOWFORM — auto-generated stub from
/// <c>src/map/skills/thief/shadowform.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ShadowForm : SkillImpl
{
    public ShadowForm() : base(SkillIds.SC_SHADOWFORM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if( sd && dstsd && src != target && !dstsd->shadowform_id ) {
    // 		if( clif_skill_nodamage(src,*target,getSkillId(),skill_lv,sc_start4(src,src,type,100,skill_lv,target->id,4+skill_lv,0,skill_get_time(getSkillId(), skill_lv))) )
    // 			dstsd->shadowform_id = src->id;
    // 	}
    // 	else if( sd )
    // 		clif_skill_fail( *sd, getSkillId() );
    }
}
