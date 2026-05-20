using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_ANTENPOU — auto-generated stub from
/// <c>src/map/skills/ninja/darkeningcannon.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class DarkeningCannon : SkillImpl
{
    public DarkeningCannon() : base(SkillIds.SS_ANTENPOU) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, skill_get_sc(getSkillId()), 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 450 + 950 * skill_lv;
    // 	skillratio += 5 * sstatus->spl;
    // 	RE_LVL_DMOD(100);
    // 	if (mflag & SKILL_ALTDMG_FLAG)
    // 		skillratio = skillratio * 3 / 10;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (flag & 1)
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_mirage_cast(*src, nullptr,getSkillId(), skill_lv, 0, 0, tick, flag | BCT_WOS);
    // 	int32 range = skill_get_splash( getSkillId(), skill_lv );
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 
    // 	map_foreachinrange( skill_area_sub, target, range, BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id );
    }
}
