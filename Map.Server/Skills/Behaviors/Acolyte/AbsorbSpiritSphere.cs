using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_ABSORBSPIRITS — auto-generated stub from
/// <c>src/map/skills/acolyte/absorbspiritsphere.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AbsorbSpiritSphere : SkillImpl
{
    public AbsorbSpiritSphere() : base(SkillIds.MO_ABSORBSPIRITS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 	mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 	status_data* tstatus = status_get_status_data(*target);
    // 
    // 	int32 i = 0;
    // 	if (dstsd && (battle_check_target(src, target, BCT_SELF) > 0 || battle_check_target(src, target, BCT_ENEMY) > 0) && // Only works on self and enemies
    // 		(dstsd->class_&MAPID_FIRSTMASK) != MAPID_GUNSLINGER ) { // split the if for readability, and included gunslingers in the check so that their coins cannot be removed [Reddozen]
    // 		if (dstsd->spiritball > 0) {
    // 			i = dstsd->spiritball * 7;
    // 			pc_delspiritball(dstsd,dstsd->spiritball,0);
    // 		}
    // 		if (dstsd->spiritcharm_type != CHARM_TYPE_NONE && dstsd->spiritcharm > 0) {
    // 			i += dstsd->spiritcharm * 7;
    // 			pc_delspiritcharm(dstsd,dstsd->spiritcharm,dstsd->spiritcharm_type);
    // 		}
    // 	} else if (dstmd && !status_has_mode(tstatus,MD_STATUSIMMUNE) && rnd() % 100 < 20) { // check if target is a monster and not status immune, for the 20% chance to absorb 2 SP per monster's level [Reddozen]
    // 		i = 2 * dstmd->level;
    // 		mob_target(dstmd,src,0);
    // 	} else {
    // 		if (sd)
    // 			clif_skill_fail( *sd, getSkillId() );
    // 		return;
    // 	}
    // 	if (i) status_heal(src, 0, i, 3);
    // 	clif_skill_nodamage(src,*target,getSkillId(),skill_lv,i != 0);
    }
}
