using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_INVISIBILITY — auto-generated stub from
/// <c>src/map/skills/thief/invisibility.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Invisibility : SkillImpl
{
    public Invisibility() : base(SkillIds.SC_INVISIBILITY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	status_change *tsc = status_get_sc(target);
    // 	status_change_entry *tsce = (tsc != nullptr && type != SC_NONE) ? tsc->getSCE(type) : nullptr;
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 	int32 i = 0;
    // 
    // 	if (tsce) {
    // 		i = status_change_end(target, type);
    // 		if( i )
    // 			clif_skill_nodamage(src,*target,getSkillId(),-1,i);
    // 		else if( sd )
    // 			clif_skill_fail( *sd, getSkillId() );
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 	i = sc_start(src,target,type,100,skill_lv,skill_get_time(getSkillId(),skill_lv));
    // 	if( i )
    // 		clif_skill_nodamage(src,*target,getSkillId(),-1,i);
    // 	else if( sd )
    // 		clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL_LEVEL );
    }
}
