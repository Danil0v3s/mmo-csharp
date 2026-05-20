using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SJ_FLASHKICK — auto-generated stub from
/// <c>src/map/skills/taekwon/flashkick.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FlashKick : SkillImpl
{
    public FlashKick() : base(SkillIds.SJ_FLASHKICK) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* md = BL_CAST(BL_MOB, src);
    // 	mob_data* tmd = BL_CAST(BL_MOB, target);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* tsd = BL_CAST(BL_PC, target);
    // 
    // 	// Only players and monsters can be tagged....I think??? [Rytech]
    // 	// Lets only allow players and monsters to use this skill for safety reasons.
    // 	if ((!tsd && !tmd) || !sd && !md) {
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 		return;
    // 	}
    // 
    // 	// Check if the target is already tagged by another source.
    // 	if ((tsd && tsd->sc.getSCE(SC_FLASHKICK) && tsd->sc.getSCE(SC_FLASHKICK)->val1 != src->id) || (tmd && tmd->sc.getSCE(SC_FLASHKICK) && tmd->sc.getSCE(SC_FLASHKICK)->val1 != src->id)) { // Same as the above check, but for monsters.
    // 		// Can't tag a player that was already tagged from another source.
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 
    // 	if (sd) { // Tagging the target.
    // 		int32 i;
    // 
    // 		ARR_FIND(0, MAX_STELLAR_MARKS, i, sd->stellar_mark[i] == target->id);
    // 		if (i == MAX_STELLAR_MARKS) {
    // 			ARR_FIND(0, MAX_STELLAR_MARKS, i, sd->stellar_mark[i] == 0);
    // 			if (i == MAX_STELLAR_MARKS) { // Max number of targets tagged. Fail the skill.
    // 				clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    // 				flag |= SKILL_NOCONSUME_REQ;
    // 				return;
    // 			}
    // 		}
    // 
    // 		// Tag the target only if damage was done. If it deals no damage, it counts as a miss and won't tag.
    // 		// Note: Not sure if it works like this in official but you can't mark on something you can't
    // 		// hit, right? For now well just use this logic until we can get a confirm on if it does this or not. [Rytech]
    // 		if (skill_attack(BF_WEAPON, src, src, target, getSkillId(), skill_lv, tick, flag) > 0) { // Add the ID of the tagged target to the player's tag list and start the status on the target.
    // 			sd->stellar_mark[i] = target->id;
    // 
    // 			// Val4 flags if the status was applied by a player or a monster.
    // 			// This will be important for other skills that work together with this one.
    // 			// 1 = Player, 2 = Monster.
    // 			// Note: Because the attacker's ID and the slot number is handled here, we have to
    // 			// apply the status here. We can't pass this data to skill_additional_effect.
    // 			sc_start4(src, target, SC_FLASHKICK, 100, src->id, i, skill_lv, 1, skill_get_time(getSkillId(), skill_lv));
    // 		}
    // 	} else if (md) { // Monsters can't track with this skill. Just give the status.
    // 		if (skill_attack(BF_WEAPON, src, src, target, getSkillId(), skill_lv, tick, flag) > 0)
    // 			sc_start4(src, target, SC_FLASHKICK, 100, 0, 0, skill_lv, 2, skill_get_time(getSkillId(), skill_lv));
    // 	}
    }
}
