using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_MANDRAGORA — auto-generated stub from
/// <c>src/map/skills/npc/npchowlingofmandragora.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NpcHowlingOfMandragora : SkillImpl
{
    public NpcHowlingOfMandragora() : base(SkillIds.NPC_MANDRAGORA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* tstatus = status_get_status_data(*target);
    // 	status_change *tsc = status_get_sc(target);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	if( flag&1 ) {
    // 		int32 rate;
    // 		rate = (20 * skill_lv) - (tstatus->vit + tstatus->luk) / 5;
    // 
    // 		if (rate < 10)
    // 			rate = 10;
    // 		if (target->type == BL_MOB || (tsc && tsc->getSCE(type)))
    // 			return; // Don't activate if target is a monster or zap SP if target already has Mandragora active.
    // 		if (rnd()%100 < rate) {
    // 			sc_start(src,target,type,100,skill_lv,skill_get_time(getSkillId(),skill_lv));
    // 			status_zap(target,0,status_get_max_sp(target) * (25 + 5 * skill_lv) / 100);
    // 		}
    // 	} else {
    // 		map_foreachinallrange(skill_area_sub,target,skill_get_splash(getSkillId(),skill_lv),BL_CHAR,src,getSkillId(),skill_lv,tick,flag|BCT_ENEMY|1,skill_castend_nodamage_id);
    // 		clif_skill_nodamage(src,*src,getSkillId(),skill_lv);
    // 	}
    }
}
