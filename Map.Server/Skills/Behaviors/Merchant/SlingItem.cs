using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_SLINGITEM — auto-generated stub from
/// <c>src/map/skills/merchant/slingitem.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SlingItem : SkillImpl
{
    public SlingItem() : base(SkillIds.GN_SLINGITEM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 	int32 i = 0;
    // 
    // 	if( sd ) {
    // 		i = sd->equip_index[EQI_AMMO];
    // 		if( i < 0 )
    // 			return; // No ammo.
    // 		t_itemid ammo_id = sd->inventory_data[i]->nameid;
    // 		if( ammo_id == 0 )
    // 			return;
    // 		sd->itemid = ammo_id;
    // 		if( itemdb_group.item_exists(IG_BOMB, ammo_id) ) {
    // 			if(battle_check_target(src,target,BCT_ENEMY) > 0) {// Only attack if the target is an enemy.
    // 				if( ammo_id == ITEMID_PINEAPPLE_BOMB )
    // 					map_foreachincell(skill_area_sub,target->m,target->x,target->y,BL_CHAR,src,GN_SLINGITEM_RANGEMELEEATK,skill_lv,tick,flag|BCT_ENEMY|1,skill_castend_damage_id);
    // 				else
    // 					skill_attack(BF_WEAPON,src,src,target,GN_SLINGITEM_RANGEMELEEATK,skill_lv,tick,flag);
    // 			} else //Otherwise, it fails, shows animation and removes items.
    // 				clif_skill_fail( *sd, GN_SLINGITEM_RANGEMELEEATK, USESKILL_FAIL );
    // 		} else if (itemdb_group.item_exists(IG_THROWABLE, ammo_id)) {
    // 			switch (ammo_id) {
    // 				case ITEMID_HP_INC_POTS_TO_THROW: // MaxHP +(500 + Thrower BaseLv * 10 / 3) and heals 1% MaxHP
    // 					sc_start2(src, target, SC_PROMOTE_HEALTH_RESERCH, 100, 2, 1, 500000);
    // 					status_percent_heal(target, 1, 0);
    // 					break;
    // 				case ITEMID_HP_INC_POTM_TO_THROW: // MaxHP +(1500 + Thrower BaseLv * 10 / 3) and heals 2% MaxHP
    // 					sc_start2(src, target, SC_PROMOTE_HEALTH_RESERCH, 100, 2, 2, 500000);
    // 					status_percent_heal(target, 2, 0);
    // 					break;
    // 				case ITEMID_HP_INC_POTL_TO_THROW: // MaxHP +(2500 + Thrower BaseLv * 10 / 3) and heals 5% MaxHP
    // 					sc_start2(src, target, SC_PROMOTE_HEALTH_RESERCH, 100, 2, 3, 500000);
    // 					status_percent_heal(target, 5, 0);
    // 					break;
    // 				case ITEMID_SP_INC_POTS_TO_THROW: // MaxSP +(Thrower BaseLv / 10 - 5)% and recovers 2% MaxSP
    // 					sc_start2(src, target, SC_ENERGY_DRINK_RESERCH, 100, 2, 1, 500000);
    // 					status_percent_heal(target, 0, 2);
    // 					break;
    // 				case ITEMID_SP_INC_POTM_TO_THROW: // MaxSP +(Thrower BaseLv / 10)% and recovers 4% MaxSP
    // 					sc_start2(src, target, SC_ENERGY_DRINK_RESERCH, 100, 2, 2, 500000);
    // 					status_percent_heal(target, 0, 4);
    // 					break;
    // 				case ITEMID_SP_INC_POTL_TO_THROW: // MaxSP +(Thrower BaseLv / 10 + 5)% and recovers 8% MaxSP
    // 					sc_start2(src, target, SC_ENERGY_DRINK_RESERCH, 100, 2, 3, 500000);
    // 					status_percent_heal(target, 0, 8);
    // 					break;
    // 				default:
    // 					if (dstsd)
    // 						run_script(sd->inventory_data[i]->script, 0, dstsd->id, fake_nd->id);
    // 					break;
    // 			}
    // 		}
    // 	}
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv);// This packet is received twice actually, I think it is to show the animation.
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    //     // (empty / no-op in rAthena)
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    //     // (empty / no-op in rAthena)
    return baseRatio;
    }
}
