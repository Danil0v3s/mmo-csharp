using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ST_FULLSTRIP — auto-generated stub from
/// <c>src/map/skills/thief/divestall.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DivestAll : SkillImpl
{
    public DivestAll() : base(SkillIds.ST_FULLSTRIP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 	map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	bool i;
    // 
    // 	//Special message when trying to use strip on FCP [Jobbie]
    // 	if( sd && tsc && tsc->getSCE(SC_CP_WEAPON) && tsc->getSCE(SC_CP_HELM) && tsc->getSCE(SC_CP_ARMOR) && tsc->getSCE(SC_CP_SHIELD))
    // 	{
    // 		clif_gospel_info( *sd, 0x28 );
    // 		return;
    // 	}
    // 
    // 	if( i = skill_strip_equip(src, target, getSkillId(), skill_lv) )
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv,i);
    // 
    // 	//Nothing stripped.
    // 	if( sd && !i )
    // 		clif_skill_fail( *sd, getSkillId() );
    }
}
