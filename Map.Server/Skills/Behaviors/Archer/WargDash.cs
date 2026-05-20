using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGDASH — auto-generated stub from
/// <c>src/map/skills/archer/wargdash.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WargDash : RecursiveDamageSplashSkillImpl
{
    public WargDash() : base(SkillIds.RA_WUGDASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // ATK 300%
    // 	base_skillratio += 200;
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 	sc_type type = skill_get_sc(getSkillId());
    // 	status_change *tsc = status_get_sc(target);
    // 	status_change_entry *tsce = (tsc && type != SC_NONE)?tsc->getSCE(type):nullptr;
    // 
    // 	if( tsce ) {
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv,status_change_end(target, type));
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 	if( sd && pc_isridingwug(sd) ) {
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv,sc_start4(src,target,type,100,skill_lv,unit_getdir(target),0,0,0));
    // 		clif_walkok(*sd);
    // 	}
    }
}
