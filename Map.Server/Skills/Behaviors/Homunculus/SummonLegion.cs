using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_SUMMON_LEGION — auto-generated stub from
/// <c>src/map/skills/homunculus/homunculus_summonlegion.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SummonLegion : SkillImpl
{
    public SummonLegion() : base(SkillIds.MH_SUMMON_LEGION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 summons[5] = { MOBID_S_HORNET, MOBID_S_GIANT_HORNET, MOBID_S_GIANT_HORNET, MOBID_S_LUCIOLA_VESPA, MOBID_S_LUCIOLA_VESPA };
    // 	int32 qty[5] = { 3, 3, 4, 4, 5 };
    // 	int32 count = 0;
    // 	int32 maxcount = qty[skill_lv - 1];
    // 
    // 	map_foreachinmap(summon_legion_count_sub, src->m, BL_MOB, src->id, summons[skill_lv - 1], &count);
    // 	if (count >= maxcount) {
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return; //max qty already spawned
    // 	}
    // 
    // 	for (int32 i_slave = 0; i_slave < qty[skill_lv - 1]; i_slave++) { //easy way
    // 		mob_data *sum_md = mob_once_spawn_sub(src, src->m, src->x, src->y, status_get_name(*src), summons[skill_lv - 1], "", SZ_SMALL, AI_ATTACK);
    // 		if (sum_md) {
    // 			sum_md->master_id = src->id;
    // 			sum_md->special_state.ai = AI_LEGION;
    // 			if (sum_md->deletetimer != INVALID_TIMER) {
    // 				delete_timer(sum_md->deletetimer, mob_timer_delete);
    // 			}
    // 			sum_md->deletetimer = add_timer(gettick() + skill_get_time(getSkillId(), skill_lv), mob_timer_delete, sum_md->id, 0);
    // 			mob_spawn(sum_md); //Now it is ready for spawning.
    // 			sc_start4(sum_md, sum_md, SC_MODECHANGE, 100, 1, 0, MD_CANATTACK | MD_AGGRESSIVE, 0, 60000);
    // 		}
    // 	}
    }
}
