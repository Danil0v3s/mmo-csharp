using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGRIDER — auto-generated stub from
/// <c>src/map/skills/archer/wargrider.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WargRider : SkillImpl
{
    public WargRider() : base(SkillIds.RA_WUGRIDER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if( sd ) {
    // 		if( !pc_isridingwug(sd) && pc_iswug(sd) ) {
    // 			pc_setoption(sd,sd->sc.option&~OPTION_WUG);
    // 			pc_setoption(sd,sd->sc.option|OPTION_WUGRIDER);
    // 		} else if( pc_isridingwug(sd) ) {
    // 			pc_setoption(sd,sd->sc.option&~OPTION_WUGRIDER);
    // 			pc_setoption(sd,sd->sc.option|OPTION_WUG);
    // 		}
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	}
    }
}
