using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_WILD_FIRE — auto-generated stub from
/// <c>src/map/skills/gunslinger/wildfire.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WildFire : WeaponSkillImpl
{
    public WildFire() : base(SkillIds.NW_WILD_FIRE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	status_change* sc = status_get_sc(src);
    // 
    // 	int32 i = skill_get_splash(getSkillId(), skill_lv);
    // 	if (sd && sd->status.weapon == W_GRENADE)
    // 		i += 2;
    // 	map_foreachinallarea(skill_area_sub,
    // 		src->m, x - i, y - i, x + i, y + i, BL_CHAR,
    // 		src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | 1,
    // 		skill_castend_damage_id);
    // 	if (sc && sc->getSCE(SC_INTENSIVE_AIM_COUNT))
    // 		status_change_end(src, SC_INTENSIVE_AIM_COUNT);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* sc = status_get_sc(src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 1500 + 3000 * skill_lv;
    // 	skillratio += 5 * sstatus->con;
    // 	if (sc && sc->getSCE(SC_INTENSIVE_AIM_COUNT))
    // 		skillratio += sc->getSCE(SC_INTENSIVE_AIM_COUNT)->val1 * 500 * skill_lv;
    // 	if (sd && sd->weapontype1 == W_SHOTGUN)
    // 		skillratio += 200 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
