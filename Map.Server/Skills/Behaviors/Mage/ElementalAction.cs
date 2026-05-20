using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EL_ACTION — auto-generated stub from
/// <c>src/map/skills/mage/elementalaction.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ElementalAction : SkillImpl
{
    public ElementalAction() : base(SkillIds.SO_EL_ACTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if( sd ) {
    // 		int32 duration = 3000;
    // 		if( !sd->ed )
    // 			return;
    // 		switch(sd->ed->db->class_) {
    // 			case ELEMENTALID_AGNI_M: case ELEMENTALID_AQUA_M:
    // 			case ELEMENTALID_VENTUS_M: case ELEMENTALID_TERA_M:
    // 				duration = 6000;
    // 				break;
    // 			case ELEMENTALID_AGNI_L: case ELEMENTALID_AQUA_L:
    // 			case ELEMENTALID_VENTUS_L: case ELEMENTALID_TERA_L:
    // 				duration = 9000;
    // 				break;
    // 		}
    // 		sd->skill_id_old = getSkillId();
    // 		elemental_action(sd->ed, target, tick);
    // 		clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 		skill_blockpc_start(*sd, getSkillId(), duration);
    // 	}
    }
}
