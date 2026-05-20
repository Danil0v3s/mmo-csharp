using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_SELFDESTRUCTION — auto-generated stub from
/// <c>src/map/skills/npc/suicidebombing.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SuicideBombing : SkillImpl
{
    public SuicideBombing() : base(SkillIds.NPC_SELFDESTRUCTION) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 
    // 	if( tsc && tsc->getSCE(SC_HIDING) )
    // 		return;
    // 	if (src != target)
    // 		skill_attack(skill_get_type(getSkillId()),src,src,target,getSkillId(),skill_lv,tick,flag);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // mob_data* md = BL_CAST(BL_MOB, src);
    // 	status_data* sstatus = status_get_status_data(*src);
    // 	int32 i = 0;
    // 
    // 	//Self Destruction hits everyone in range (allies+enemies)
    // 	//Except for Summoned Marine spheres on non-versus maps, where it's just enemies and your own slaves.
    // 	if ((md == nullptr || md->special_state.ai == AI_SPHERE) && !map_flag_vs(src->m)) {
    // 		// Enable Marine Spheres to damage own Homunculus and summons outside PVP
    // 		if (battle_config.alchemist_summon_setting&8)
    // 			i = BCT_ENEMY|BCT_SLAVE;
    // 		else
    // 			i = BCT_ENEMY;
    // 	} else {
    // 		i = BCT_ALL;
    // 	}
    // 	clif_skill_nodamage(src, *src, getSkillId(), skill_lv);
    // 	map_delblock(src); //Required to prevent chain-self-destructions hitting back.
    // 	map_foreachinshootrange(skill_area_sub, target,
    // 		skill_get_splash(getSkillId(), skill_lv), BL_CHAR|BL_SKILL,
    // 		src, getSkillId(), skill_lv, tick, flag|i,
    // 		skill_castend_damage_id);
    // 	if(map_addblock(src)) {
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 	// Won't display the damage, but drop items and give exp
    // 	status_zap(src, sstatus->hp, 0, 0);
    }
}
