using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_STRIP_SHADOW — auto-generated stub from
/// <c>src/map/skills/thief/stripshadow.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class StripShadow : SkillImpl
{
    public StripShadow() : base(SkillIds.ABC_STRIP_SHADOW) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	bool strip_success = skill_strip_equip(src, target, getSkillId(), skill_lv);
    // 
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv,strip_success);
    // 
    // 	//Nothing stripped.
    // 	if( sd && !strip_success )
    // 		clif_skill_fail( *sd, getSkillId() );
    }
}
