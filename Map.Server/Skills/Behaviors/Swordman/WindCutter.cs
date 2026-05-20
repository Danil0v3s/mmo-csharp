using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_WINDCUTTER — auto-generated stub from
/// <c>src/map/skills/swordman/windcutter.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WindCutter : RecursiveDamageSplashSkillImpl
{
    public WindCutter() : base(SkillIds.RK_WINDCUTTER) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, &src);
    // 
    // 	if (sd != nullptr) {
    // 		if (sd->status.weapon == W_1HSPEAR || sd->status.weapon == W_2HSPEAR)
    // 			dmg.flag |= BF_LONG;
    // 
    // 		if (sd->weapontype1 == W_2HSWORD)
    // 			dmg.div_ = 2;
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd) {
    // 		if (sd->weapontype1 == W_2HSWORD)
    // 			skillratio += -100 + 250 * skill_lv;
    // 		else if (sd->weapontype1 == W_1HSPEAR || sd->weapontype1 == W_2HSPEAR)
    // 			skillratio += -100 + 400 * skill_lv;
    // 		else
    // 			skillratio += -100 + 300 * skill_lv;
    // 	} else
    // 		skillratio += -100 + 300 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src,*target,getSkillId(),skill_lv);
    // 	skill_castend_damage_id(src, target, getSkillId(), skill_lv, tick, flag);
    // 
    // 	if (skill_area_temp[2] == 0) {
    // 		clif_skill_damage( *src, *src, tick, status_get_amotion(src), 0, DMGVAL_IGNORE, 1, getSkillId(), skill_lv, DMG_SINGLE );
    // 	}
    }
}
