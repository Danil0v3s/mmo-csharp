using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_MAGMA_ERUPTION — auto-generated stub from
/// <c>src/map/skills/merchant/magmaeruption.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MagmaEruption : WeaponSkillImpl
{
    public MagmaEruption() : base(SkillIds.NC_MAGMA_ERUPTION) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Stun effect from 'slam'
    // 	sc_start(src, target, SC_STUN, 90, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // 'Slam' damage
    // 	base_skillratio += 350 + 50 * skill_lv;
    return baseRatio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // 1st, AoE 'slam' damage
    // 	int32 i = skill_get_splash(getSkillId(), skill_lv);
    // 	map_foreachinarea(skill_area_sub, src->m, x-i, y-i, x+i, y+i, BL_CHAR,
    // 		src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|SD_ANIMATION|1, skill_castend_damage_id);
    // 	// 2nd, AoE 'eruption' unit
    // 	skill_addtimerskill(src,tick + status_get_amotion(src) * 2,0,x,y,getSkillId(),skill_lv,0,flag);
    }


}
