using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_BENEDICTIO — auto-generated stub from
/// <c>src/map/skills/acolyte/bssacramenti.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class BenedictioSanctissimiSacramenti : SkillImpl
{
    public BenedictioSanctissimiSacramenti() : base(SkillIds.PR_BENEDICTIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* tstatus = status_get_status_data(*target);
    // 
    // 	if (!battle_check_undead(tstatus->race, tstatus->def_ele) && tstatus->race != RC_DEMON)
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv, sc_start(src, target, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time(getSkillId(), skill_lv)));
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* tstatus = status_get_status_data(*target);
    // 
    // 	//Should attack undead and demons. [Skotlex]
    // 	if (battle_check_undead(tstatus->race, tstatus->def_ele) || tstatus->race == RC_DEMON)
    // 		skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_area_temp[1] = src->id;
    // 	int32 i = skill_get_splash(getSkillId(), skill_lv);
    // 	map_foreachinallarea(skill_area_sub,
    // 		src->m, x-i, y-i, x+i, y+i, BL_PC,
    // 		src, getSkillId(), skill_lv, tick, flag|BCT_ALL|1,
    // 		skill_castend_nodamage_id);
    // 	map_foreachinallarea(skill_area_sub,
    // 		src->m, x-i, y-i, x+i, y+i, BL_CHAR,
    // 		src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|1,
    // 		skill_castend_damage_id);
    }
}
