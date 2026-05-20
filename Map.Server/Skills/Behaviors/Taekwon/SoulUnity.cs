using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SP_SOULUNITY — auto-generated stub from
/// <c>src/map/skills/taekwon/soulunity.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SoulUnity : SkillImpl
{
    public SoulUnity() : base(SkillIds.SP_SOULUNITY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 	map_session_data* sd = BL_CAST( BL_PC, src );
    // 	map_session_data* dstsd = BL_CAST( BL_PC, target );
    // 	int32 i = 0;
    // 
    // 	int8 count = min(5 + skill_lv, MAX_UNITED_SOULS);
    // 
    // 	if (sd == nullptr || sd->status.party_id == 0 || (flag & 1)) {
    // 		if (!dstsd || !sd) { // Only put player's souls in unity.
    // 			if (sd)
    // 				clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 			return;
    // 		}
    // 
    // 		if (dstsd->sc.getSCE(type) && dstsd->sc.getSCE(type)->val2 != src->id) { // Fail if a player is in unity with another source.
    // 			if (sd)
    // 				clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 			flag |= SKILL_NOCONSUME_REQ;
    // 			return;
    // 		}
    // 
    // 		if (sd) { // Unite player's soul with caster's soul.
    // 			i = 0;
    // 
    // 			ARR_FIND(0, count, i, sd->united_soul[i] == target->id);
    // 			if (i == count) {
    // 				ARR_FIND(0, count, i, sd->united_soul[i] == 0);
    // 				if(i == count) { // No more free slots? Fail the skill.
    // 					clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 					flag |= SKILL_NOCONSUME_REQ;
    // 					return;
    // 				}
    // 			}
    // 
    // 			sd->united_soul[i] = target->id;
    // 		}
    // 
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv, sc_start4(src, target, type, 100, skill_lv, src->id, i, 0, skill_get_time(getSkillId(), skill_lv)));
    // 	} else if (sd)
    // 		party_foreachsamemap(skill_area_sub, sd, skill_get_splash(getSkillId(), skill_lv), src, getSkillId(), skill_lv, tick, flag|BCT_PARTY|1, skill_castend_nodamage_id);
    }
}
