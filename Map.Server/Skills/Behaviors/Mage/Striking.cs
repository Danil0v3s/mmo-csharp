using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_STRIKING — auto-generated stub from
/// <c>src/map/skills/mage/striking.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Striking : SkillImpl
{
    public Striking() : base(SkillIds.SO_STRIKING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if (battle_check_target(src, target, BCT_SELF|BCT_PARTY) > 0) {
    // 		int32 bonus = 0;
    // 
    // 		if (dstsd) {
    // 			int16 index = dstsd->equip_index[EQI_HAND_R];
    // 
    // 			if (index >= 0 && dstsd->inventory_data[index] && dstsd->inventory_data[index]->type == IT_WEAPON)
    // 				bonus = (20 * skill_lv) * dstsd->inventory_data[index]->weapon_level;
    // 		}
    // 
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv, sc_start2(src,target, type, 100, skill_lv, bonus, skill_get_time(getSkillId(), skill_lv)));
    // 	} else if (sd)
    // 		clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL_TOTARGET );
    }
}
