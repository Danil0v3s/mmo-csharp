using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_VIOLENT_QUAKE — auto-generated stub from
/// <c>src/map/skills/mage/violentquake.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ViolentQuake : SkillImpl
{
    public ViolentQuake() : base(SkillIds.AG_VIOLENT_QUAKE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_type type = skill_get_sc(getSkillId());
    // 
    // 	sc_start(src, target, type, 100, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 
    // 	int32 area = skill_get_splash(getSkillId(), skill_lv);
    // 	int32 unit_time = skill_get_time(getSkillId(), skill_lv);
    // 	int32 unit_interval = skill_get_unit_interval(getSkillId());
    // 	uint16 tmpx = 0, tmpy = 0, climax_lv = 0;
    // 	int32 i = 0;
    // 
    // 	// Grab Climax's effect level if active.
    // 	if (sc && sc->getSCE(SC_CLIMAX))
    // 		climax_lv = sc->getSCE(SC_CLIMAX)->val1;
    // 
    // 	// Fixes rising rocks spawn area to 7x7.
    // 	if (climax_lv == 5)
    // 		area = 3;
    // 
    // 	// Displays the earthquake.
    // 	skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0);
    // 
    // 	if (climax_lv == 4) { // Deals no damage and instead inflicts a status on the enemys in range.
    // 		i = skill_get_splash(getSkillId(), skill_lv);
    // 		map_foreachinallarea(skill_area_sub, src->m, x - i, y - i, x + i, y + i, BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | 1, skill_castend_nodamage_id);
    // 	} else for (i = 1; i <= unit_time / unit_interval; i++) { // Spawn the rising rocks on random spots at seperate intervals
    // 		tmpx = x - area + rnd() % (area * 2 + 1);
    // 		tmpy = y - area + rnd() % (area * 2 + 1);
    // 		skill_unitsetting(src, AG_VIOLENT_QUAKE_ATK, skill_lv, tmpx, tmpy, flag + i * unit_interval);
    // 
    // 		if (climax_lv == 1) { // Spwan a 2nd rising rock along with the 1st one.
    // 			tmpx = x - area + rnd() % (area * 2 + 1);
    // 			tmpy = y - area + rnd() % (area * 2 + 1);
    // 			skill_unitsetting(src, AG_VIOLENT_QUAKE_ATK, skill_lv, tmpx, tmpy, flag + i * unit_interval);
    // 		}
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    //     // (empty / no-op in rAthena)
    return baseRatio;
    }
}
