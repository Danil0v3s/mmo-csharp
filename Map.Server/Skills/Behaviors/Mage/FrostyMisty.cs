using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_FROSTMISTY — auto-generated stub from
/// <c>src/map/skills/mage/frostymisty.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FrostyMisty : SkillImpl
{
    public FrostyMisty() : base(SkillIds.WL_FROSTMISTY) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skillratio += -100 + 200 + 100 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Causes Freezing status through walls.
    // 	sc_start(src, target, SC_FREEZING, 25 + 5 * skill_lv, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	sc_start(src, target, SC_MISTY_FROST, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // 	// Doesn't deal damage through non-shootable walls.
    // 	if( !battle_config.skill_wall_check || (battle_config.skill_wall_check && path_search(nullptr,src->m,src->x,src->y,target->x,target->y,1,CELL_CHKWALL)) )
    // 		skill_attack(BF_MAGIC,src,src,target,getSkillId(),skill_lv,tick,flag|SD_ANIMATION);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 i = 0;
    // 
    // 	// Cast center might be relevant later (e.g. for knockback direction)
    // 	skill_area_temp[4] = x;
    // 	skill_area_temp[5] = y;
    // 	i = skill_get_splash(getSkillId(),skill_lv);
    // 	map_foreachinarea(skill_area_sub,src->m,x-i,y-i,x+i,y+i,BL_CHAR|BL_SKILL,src,getSkillId(),skill_lv,tick,flag|BCT_ENEMY|1,skill_castend_damage_id);
    }
}
