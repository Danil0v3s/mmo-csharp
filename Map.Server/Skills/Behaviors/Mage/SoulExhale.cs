using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// PF_SOULCHANGE — auto-generated stub from
/// <c>src/map/skills/mage/soulexhale.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SoulExhale : SkillImpl
{
    public SoulExhale() : base(SkillIds.PF_SOULCHANGE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 	status_change* tsc = status_get_sc(target);
    // 	mob_data* dstmd = BL_CAST(BL_MOB, target);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 	uint32 sp1 = 0, sp2 = 0;
    // 
    // 	if (dstmd != nullptr) {
    // 		if (dstmd->state.soul_change_flag) {
    // 			if (sd != nullptr) {
    // 				clif_skill_fail(*sd, getSkillId());
    // 			}
    // 			return;
    // 		}
    // 
    // 		dstmd->state.soul_change_flag = 1;
    // 		sp2 = sstatus->max_sp * 3 / 100;
    // 		status_heal(src, 0, sp2, 2);
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		return;
    // 	}
    // 
    // 	sp1 = sstatus->sp;
    // 	sp2 = tstatus->sp;
    // #ifdef RENEWAL
    // 	sp1 /= 2;
    // 	sp2 /= 2;
    // 	if (tsc != nullptr && tsc->hasSCE(SC_EXTREMITYFIST)) {
    // 		sp1 = tstatus->sp;
    // 	}
    // #endif
    // 	if (tsc != nullptr && tsc->hasSCE(SC_NORECOVER_STATE)) {
    // 		sp1 = tstatus->sp;
    // 	}
    // 
    // 	status_set_sp(src, sp2, 3);
    // 	status_set_sp(target, sp1, 3);
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    }
}
