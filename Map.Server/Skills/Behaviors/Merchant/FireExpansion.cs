using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_FIRE_EXPANSION — auto-generated stub from
/// <c>src/map/skills/merchant/fireexpansion.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FireExpansion : SkillImpl
{
    public FireExpansion() : base(SkillIds.GN_FIRE_EXPANSION) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	struct unit_data* ud = unit_bl2ud(src);
    // 
    // 	if (!ud)
    // 		return;
    // 
    // 	auto predicate = [x, y](std::shared_ptr<s_skill_unit_group> sg) { auto* su = sg->unit; return sg->skill_id == GN_DEMONIC_FIRE && distance_xy(x, y, su->x, su->y) < 4; };
    // 	auto it = std::find_if(ud->skillunits.begin(), ud->skillunits.end(), predicate);
    // 	if (it != ud->skillunits.end()) {
    // 		auto* unit_group = it->get();
    // 		skill_unit* su = unit_group->unit;
    // 
    // 		switch (skill_lv) {
    // 		case 1: {
    // 			// TODO:
    // 			int32 duration = (int32)(unit_group->limit - DIFF_TICK(tick, unit_group->tick));
    // 
    // 			skill_delunit(su);
    // 			skill_unitsetting(src, GN_DEMONIC_FIRE, 1, x, y, duration);
    // 			flag |= 1;
    // 		}
    // 				break;
    // 		case 2:
    // 			map_foreachinallarea(skill_area_sub, src->m, su->x - 2, su->y - 2, su->x + 2, su->y + 2, BL_CHAR, src, GN_DEMONIC_FIRE, skill_lv + 20, tick, flag | BCT_ENEMY | SD_LEVEL | 1, skill_castend_damage_id);
    // 			if (su != nullptr)
    // 				skill_delunit(su);
    // 			break;
    // 		case 3:
    // 			skill_delunit(su);
    // 			skill_unitsetting(src, GN_FIRE_EXPANSION_SMOKE_POWDER, 1, x, y, 0);
    // 			flag |= 1;
    // 			break;
    // 		case 4:
    // 			skill_delunit(su);
    // 			skill_unitsetting(src, GN_FIRE_EXPANSION_TEAR_GAS, 1, x, y, 0);
    // 			flag |= 1;
    // 			break;
    // 		case 5: {
    // 			uint16 acid_lv = 5; // Cast at Acid Demonstration at level 5 unless the user has a higher level learned.
    // 
    // 			if (sd && pc_checkskill(sd, CR_ACIDDEMONSTRATION) > 5)
    // 				acid_lv = pc_checkskill(sd, CR_ACIDDEMONSTRATION);
    // 			map_foreachinallarea(skill_area_sub, src->m, su->x - 2, su->y - 2, su->x + 2, su->y + 2, BL_CHAR, src, GN_FIRE_EXPANSION_ACID, acid_lv, tick, flag | BCT_ENEMY | SD_LEVEL | 1, skill_castend_damage_id);
    // 			if (su != nullptr)
    // 				skill_delunit(su);
    // 		}
    // 			break;
    // 		}
    // 	}
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    //     // (empty / no-op in rAthena)
    }
}
