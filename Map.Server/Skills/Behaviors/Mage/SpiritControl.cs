using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EL_CONTROL — auto-generated stub from
/// <c>src/map/skills/mage/spiritcontrol.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpiritControl : SkillImpl
{
    public SpiritControl() : base(SkillIds.SO_EL_CONTROL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( sd ) {
    // 		int32 mode;
    // 
    // 		if( !sd->ed )
    // 			return;
    // 
    // 		if( skill_lv == 4 ) {// At level 4 delete elementals.
    // 			elemental_delete(sd->ed);
    // 			return;
    // 		}
    // 		switch( skill_lv ) {// Select mode bassed on skill level used.
    // 			case 1: mode = EL_MODE_PASSIVE; break; // Standard mode.
    // 			case 2: mode = EL_MODE_ASSIST; break;
    // 			case 3: mode = EL_MODE_AGGRESSIVE; break;
    // 		}
    // 		if( !elemental_change_mode(sd->ed,mode) ) {
    // 			clif_skill_fail( *sd, getSkillId() );
    // 			return;
    // 		}
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	}
    }
}
