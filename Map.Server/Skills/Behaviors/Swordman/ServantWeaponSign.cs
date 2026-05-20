using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_SERVANT_W_SIGN — auto-generated stub from
/// <c>src/map/skills/swordman/servantweaponsign.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ServantWeaponSign : SkillImpl
{
    public ServantWeaponSign() : base(SkillIds.DK_SERVANT_W_SIGN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	mob_data* md = BL_CAST(BL_MOB, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 	mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 	status_change* tsc = status_get_sc(target);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	// Max allowed targets to be marked.
    // 	// Only players and monsters can be marked....I think??? [Rytech]
    // 	// Lets only allow players and monsters to use this skill for safety reasons.
    // 	if ((!dstsd && !dstmd) || !sd && !md) {
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 		return;
    // 	}
    // 
    // 	// Check if the target is already marked by another source.
    // 	if (tsc && tsc->getSCE(type) && tsc->getSCE(type)->val1 != src->id) {
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 
    // 		
    // 	// Mark the target.
    // 	if( sd ){
    // 		int8 i;
    // 		int8 count = MAX_SERVANT_SIGN;
    // 
    // 		ARR_FIND(0, count, i, sd->servant_sign[i] == target->id);
    // 		if (i == count) {
    // 			ARR_FIND(0, count, i, sd->servant_sign[i] == 0);
    // 			if (i == count) { // Max number of targets marked. Fail the skill.
    // 				clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 				flag |= SKILL_NOCONSUME_REQ;
    // 				return;
    // 			}
    // 
    // 			// Add the ID of the marked target to the player's sign list.
    // 			sd->servant_sign[i] = target->id;
    // 		}
    // 
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		sc_start4(src, target, type, 100, src->id, i, skill_lv, 0, skill_get_time(getSkillId(), skill_lv));
    // 	} else if (md) // Monster's cant track with this skill. Just give the status.
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv, sc_start4(src, target, type, 100, 0, 0, skill_lv, 0, skill_get_time(getSkillId(), skill_lv)));
    }
}
