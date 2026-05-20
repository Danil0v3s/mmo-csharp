using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// ML_BRANDISH — auto-generated stub from
/// <c>src/map/skills/mercenary/mercenary_brandishspear.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MercenaryBrandishSpear : SkillImpl
{
    public MercenaryBrandishSpear() : base(SkillIds.ML_BRANDISH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int32 ratio = 100 + 20 * skill_lv;
    // 
    // 	base_skillratio += -100 + ratio;
    // 	if(skill_lv > 3 && wd->miscflag == 0)
    // 		base_skillratio += ratio / 2;
    // 	if(skill_lv > 6 && wd->miscflag == 0)
    // 		base_skillratio += ratio / 4;
    // 	if(skill_lv > 9 && wd->miscflag == 0)
    // 		base_skillratio += ratio / 8;
    // 	if(skill_lv > 6 && wd->miscflag == 1)
    // 		base_skillratio += ratio / 2;
    // 	if(skill_lv > 9 && wd->miscflag == 1)
    // 		base_skillratio += ratio / 4;
    // 	if(skill_lv > 9 && wd->miscflag == 2)
    // 		base_skillratio += ratio / 2;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Coded apart for it needs the flag passed to the damage calculation.
    // 	if (skill_area_temp[1] != target->id)
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag|SD_ANIMATION);
    // 	else
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	skill_area_temp[1] = target->id;
    // 
    // 	if(skill_lv >= 10)
    // 		map_foreachindir(skill_area_sub, src->m, src->x, src->y, target->x, target->y,
    // 			skill_get_splash(getSkillId(), skill_lv), 1, skill_get_maxcount(getSkillId(), skill_lv)-1, splash_target(src),
    // 			src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | (sd?3:0),
    // 			skill_castend_damage_id);
    // 	if(skill_lv >= 7)
    // 		map_foreachindir(skill_area_sub, src->m, src->x, src->y, target->x, target->y,
    // 			skill_get_splash(getSkillId(), skill_lv), 1, skill_get_maxcount(getSkillId(), skill_lv)-2, splash_target(src),
    // 			src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | (sd?2:0),
    // 			skill_castend_damage_id);
    // 	if(skill_lv >= 4)
    // 		map_foreachindir(skill_area_sub, src->m, src->x, src->y, target->x, target->y,
    // 			skill_get_splash(getSkillId(), skill_lv), 1, skill_get_maxcount(getSkillId(), skill_lv)-3, splash_target(src),
    // 			src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | (sd?1:0),
    // 			skill_castend_damage_id);
    // 	map_foreachindir(skill_area_sub, src->m, src->x, src->y, target->x, target->y,
    // 		skill_get_splash(getSkillId(), skill_lv), skill_get_maxcount(getSkillId(), skill_lv)-3, 0, splash_target(src),
    // 		src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | 0,
    // 		skill_castend_damage_id);
    }
}
