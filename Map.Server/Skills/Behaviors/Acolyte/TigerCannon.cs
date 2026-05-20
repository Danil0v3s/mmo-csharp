using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_TIGERCANNON — auto-generated stub from
/// <c>src/map/skills/acolyte/tigercannon.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class TigerCannon : WeaponSkillImpl
{
    public TigerCannon() : base(SkillIds.SR_TIGERCANNON) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_change *sc = status_get_sc(src);
    // 	uint32 hp = sstatus->max_hp * (10 + (skill_lv * 2)) / 100;
    // 	uint32 sp = sstatus->max_sp * (5 + skill_lv) / 100;
    // 
    // 	if (wd->miscflag&8)
    // 		// Base_Damage = [((Caster consumed HP + SP) / 2) x Caster Base Level / 100] %
    // 		skillratio += -100 + (hp + sp) / 2;
    // 	else
    // 		// Base_Damage = [((Caster consumed HP + SP) / 4) x Caster Base Level / 100] %
    // 		skillratio += -100 + (hp + sp) / 4;
    // 	RE_LVL_DMOD(100);
    // 
    // 	if (sc != nullptr && sc->hasSCE(SC_GT_REVITALIZE))
    // 		skillratio += skillratio * 30 / 100;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (flag & 1) {
    // 		int32 sflag = flag|SD_ANIMATION;
    // 		WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, sflag);
    // 	} else if (sd) {
    // 		if (sc && sc->getSCE(SC_COMBO) && sc->getSCE(SC_COMBO)->val1 == SR_FALLENEMPIRE && !sc->getSCE(SC_FLASHCOMBO))
    // 			flag |= 8; // Only apply Combo bonus when Tiger Cannon is not used through Flash Combo
    // 		map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR | BL_SKILL, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id);
    // 	}
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_castend_damage_id(src, src, getSkillId(), skill_lv, tick, flag);
    }
}
