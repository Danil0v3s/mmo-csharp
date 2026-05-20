using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_LIGHTNINGLOADER — auto-generated stub from
/// <c>src/map/skills/mage/endowtornado.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class EndowTornado : SkillImpl
{
    public EndowTornado() : base(SkillIds.SA_LIGHTNINGLOADER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* sd = BL_CAST( BL_PC, src );
    // 	map_session_data* dstsd = BL_CAST( BL_PC, target );
    // 
    // 	if (dstsd && dstsd->status.weapon == W_FIST) {
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId() );
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv,false);
    // 		return;
    // 	}
    // #ifdef RENEWAL
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv, sc_start(src, target, type, 100, skill_lv, skill_get_time(getSkillId(), skill_lv)));
    // #else
    // 	// 100% success rate at lv4 & 5, but lasts longer at lv5
    // 	if(!clif_skill_nodamage(src,*target,getSkillId(),skill_lv, sc_start(src,target,type,(60+skill_lv*10),skill_lv, skill_get_time(getSkillId(),skill_lv)))) {
    // 		if (dstsd){
    // 			int16 index = dstsd->equip_index[EQI_HAND_R];
    // 			if (index != -1 && dstsd->inventory_data[index] && dstsd->inventory_data[index]->type == IT_WEAPON)
    // 				pc_unequipitem(dstsd, index, 3); //Must unequip the weapon instead of breaking it [Daegaladh]
    // 		}
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId() );
    // 	}
    // #endif
    }
}
