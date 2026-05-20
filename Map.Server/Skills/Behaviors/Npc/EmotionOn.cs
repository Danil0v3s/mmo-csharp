using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_EMOTION_ON — auto-generated stub from
/// <c>src/map/skills/npc/emotionon.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class EmotionOn : SkillImpl
{
    public EmotionOn() : base(SkillIds.NPC_EMOTION_ON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	status_change *tsc = status_get_sc(target);
    // 	status_change_entry *tsce = (tsc != nullptr && type != SC_NONE) ? tsc->getSCE(type) : nullptr;
    // 	mob_data* md = BL_CAST(BL_MOB, src);
    // 
    // 	//val[0] is the emotion to use.
    // 	//NPC_EMOTION_ON can change a mob's mode 'permanently' [Skotlex]
    // 	//val[1] 'sets' the mode
    // 	//val[2] adds to the current mode
    // 	//val[3] removes from the current mode
    // 	//val[4] if set, asks to delete the previous mode change.
    // 	if(md && md->skill_idx >= 0 && tsc)
    // 	{
    // 		clif_emotion( *target, static_cast<emotion_type>( md->db->skill[md->skill_idx]->val[0] ) );
    // 		if(md->db->skill[md->skill_idx]->val[4] && tsce)
    // 			status_change_end(target, type);
    // 
    // 		if(md->db->skill[md->skill_idx]->val[1] || md->db->skill[md->skill_idx]->val[2])
    // 			sc_start4(src,src, type, 100, skill_lv,
    // 				md->db->skill[md->skill_idx]->val[1],
    // 				md->db->skill[md->skill_idx]->val[2],
    // 				md->db->skill[md->skill_idx]->val[3],
    // 				skill_get_time(getSkillId(), skill_lv));
    // 
    // 		//Reset aggressive state depending on resulting mode
    // 		if (!battle_config.npc_emotion_behavior)
    // 			md->state.aggressive = status_has_mode(&md->status,MD_ANGRY)?1:0;
    // 	}
    }
}
