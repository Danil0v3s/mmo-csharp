using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_ELEMENTAL_BUSTER — auto-generated stub from
/// <c>src/map/skills/mage/elementalbuster.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ElementalBuster : SkillImpl
{
    public ElementalBuster() : base(SkillIds.EM_ELEMENTAL_BUSTER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd == nullptr)
    // 		return;
    // 
    // 	if (!sd->ed || !(sd->ed->elemental.class_ >= ELEMENTALID_DILUVIO && sd->ed->elemental.class_ <= ELEMENTALID_SERPENS)) {
    // 		clif_skill_fail( *sd, getSkillId() );
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 
    // 	uint16 buster_element;
    // 
    // 	switch (sd->ed->elemental.class_) {
    // 		case ELEMENTALID_ARDOR:
    // 			buster_element = EM_ELEMENTAL_BUSTER_FIRE;
    // 			break;
    // 		case ELEMENTALID_DILUVIO:
    // 			buster_element = EM_ELEMENTAL_BUSTER_WATER;
    // 			break;
    // 		case ELEMENTALID_PROCELLA:
    // 			buster_element = EM_ELEMENTAL_BUSTER_WIND;
    // 			break;
    // 		case ELEMENTALID_TERREMOTUS:
    // 			buster_element = EM_ELEMENTAL_BUSTER_GROUND;
    // 			break;
    // 		case ELEMENTALID_SERPENS:
    // 			buster_element = EM_ELEMENTAL_BUSTER_POISON;
    // 			break;
    // 	}
    // 
    // 	skill_area_temp[1] = 0;
    // 	clif_skill_nodamage(src, *target, buster_element, skill_lv);// Animation for the triggered blaster element.
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);// Triggered after blaster animation to make correct skill name scream appear.
    // 	map_foreachinrange(skill_area_sub, target, 6, BL_CHAR | BL_SKILL, src, buster_element, skill_lv, tick, flag | BCT_ENEMY | SD_LEVEL | SD_SPLASH | 1, skill_castend_damage_id);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    //     // (empty / no-op in rAthena)
    return baseRatio;
    }


}
