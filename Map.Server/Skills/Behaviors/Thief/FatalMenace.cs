using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_FATALMENACE — auto-generated stub from
/// <c>src/map/skills/thief/fatalmenace.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FatalMenace : WeaponSkillImpl
{
    public FatalMenace() : base(SkillIds.SC_FATALMENACE) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, &src);
    // 
    // 	if (sd != nullptr && sd->weapontype1 == W_DAGGER)
    // 		dmg.div_++;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 
    // 	skillratio += 120 * skill_lv + sstatus->agi; // !TODO: What's the AGI bonus?
    // 
    // 	if( sc != nullptr && sc->getSCE( SC_ABYSS_DAGGER ) ){
    // 		skillratio += 30 * skill_lv;
    // 	}
    // 
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if( flag&1 )
    // 		WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 	else {
    // 		map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), splash_target(src), src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|1, skill_castend_damage_id);
    // 		clif_skill_damage( *src, *src, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // 	}
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (skill_lv < 6)
    // 		hit_rate -= 35 - 5 * skill_lv;
    // 	else if (skill_lv > 6)
    // 		hit_rate += 5 * skill_lv - 30;
    return hitRate;
    }
}
