using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGSTRIKE — auto-generated stub from
/// <c>src/map/skills/archer/wargstrike.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WargStrike : WeaponSkillImpl
{
    public WargStrike() : base(SkillIds.RA_WUGSTRIKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += -100 + 200 * skill_lv;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if( sd && pc_isridingwug(sd) ){
    // 		uint8 dir = map_calc_dir(target, src->x, src->y);
    // 
    // 		if( unit_movepos(src, target->x+dirx[dir], target->y+diry[dir], 1, 1) ) {
    // 			clif_blown(src);
    // 			WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 		}
    // 		return;
    // 	}
    // 	if( path_search(nullptr,src->m,src->x,src->y,target->x,target->y,1,CELL_CHKNOREACH) ) {
    // 		WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 	}
    }
}
