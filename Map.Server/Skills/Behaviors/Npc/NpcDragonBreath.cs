using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_DRAGONBREATH — auto-generated stub from
/// <c>src/map/skills/npc/npcdragonbreath.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NpcDragonBreath : WeaponSkillImpl
{
    public NpcDragonBreath() : base(SkillIds.NPC_DRAGONBREATH) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (skill_lv > 5)
    // 		sc_start4(src,target,SC_FREEZING,50,skill_lv,1000,src->id,0,skill_get_time(getSkillId(),skill_lv));
    // 	else
    // 		sc_start4(src,target,SC_BURNING,50,skill_lv,1000,src->id,0,skill_get_time(getSkillId(),skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (skill_lv > 5)
    // 		base_skillratio += 500 + 500 * (skill_lv - 5);	// Level 6-10 is using water element, like RK_DRAGONBREATH_WATER
    // 	else
    // 		base_skillratio += 500 + 500 * skill_lv;	// Level 1-5 is using fire element, like RK_DRAGONBREATH
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *tsc = status_get_sc(target);
    // 
    // 	if( tsc && tsc->getSCE(SC_HIDING) )
    // 		clif_skill_nodamage(src,*src,getSkillId(),skill_lv);
    // 	else {
    // 		WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 	}
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Cast center might be relevant later (e.g. for knockback direction)
    // 	skill_area_temp[4] = x;
    // 	skill_area_temp[5] = y;
    // 	int32 i = skill_get_splash(getSkillId(),skill_lv);
    // 	map_foreachinarea(skill_area_sub,src->m,x-i,y-i,x+i,y+i,BL_CHAR|BL_SKILL,src,getSkillId(),skill_lv,tick,flag|BCT_ENEMY|1,skill_castend_damage_id);
    }
}
