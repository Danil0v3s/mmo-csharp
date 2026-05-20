using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_CHEAL — auto-generated stub from
/// <c>src/map/skills/npc/npccoluceoheal.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NpcColuceoHeal : SkillImpl
{
    public NpcColuceoHeal() : base(SkillIds.NPC_CHEAL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if( flag&1 ) {
    // 		status_data* tstatus = status_get_status_data(*target);
    // 		status_change *tsc = status_get_sc(target);
    // 
    // 		if( tstatus && !battle_check_undead(tstatus->race, tstatus->def_ele) && tsc != nullptr && !tsc->hasSCE(SC_BERSERK) ) {
    // 			int32 i = skill_calc_heal(src, target, AL_HEAL, 10, true);
    // 			if (status_isimmune(target))
    // 				i = 0;
    // 			clif_skill_nodamage(src, *target, getSkillId(), i);
    // 			if( tsc && tsc->getSCE(SC_AKAITSUKI) && i )
    // 				i = ~i + 1;
    // 			status_heal(target, i, 0, 0);
    // 		}
    // 	}
    // 	else {
    // 		map_foreachinallrange(skill_area_sub, src, skill_get_splash(getSkillId(), skill_lv), BL_MOB,
    // 			src, getSkillId(), skill_lv, tick, flag|BCT_PARTY|1, skill_castend_nodamage_id);
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	}
    }
}
