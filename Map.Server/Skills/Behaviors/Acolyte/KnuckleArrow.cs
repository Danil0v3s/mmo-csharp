using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_KNUCKLEARROW — auto-generated stub from
/// <c>src/map/skills/acolyte/knucklearrow.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class KnuckleArrow : SkillImpl
{
    public KnuckleArrow() : base(SkillIds.SR_KNUCKLEARROW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *sc = status_get_sc(src);
    // 	const map_session_data* tsd = BL_CAST(BL_PC, target);
    // 
    // 	if (wd->miscflag&4) { // ATK [(Skill Level x 150) + (1000 x Target current weight / Maximum weight) + (Target Base Level x 5) x (Caster Base Level / 150)] %
    // 		skillratio += -100 + 150 * skill_lv + status_get_lv(target) * 5;
    // 		if (tsd && tsd->weight)
    // 			skillratio += pc_getpercentweight(*tsd);
    // 		RE_LVL_DMOD(150);
    // 	} else {
    // 		if (status_get_class_(target) == CLASS_BOSS)
    // 			skillratio += 400 + 200 * skill_lv;
    // 		else // ATK [(Skill Level x 100 + 500) x Caster Base Level / 100] %
    // 			skillratio += 400 + 100 * skill_lv;
    // 		RE_LVL_DMOD(100);
    // 	}
    // 	if (sc != nullptr && sc->hasSCE(SC_GT_CHANGE))
    // 		skillratio += skillratio * 30 / 100;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Holds current direction of bl/target to src/attacker before the src is moved to bl location
    // 	dir_ka = map_calc_dir(target, src->x, src->y);
    // 	// Has slide effect
    // 	if (skill_check_unit_movepos(5, src, target->x, target->y, 1, 1))
    // 		skill_blown(src, src, 1, (dir_ka + 4) % 8, BLOWN_NONE); // Target position is actually one cell next to the target
    // 	skill_addtimerskill(src, tick + 300, target->id, 0, 0, getSkillId(), skill_lv, BF_WEAPON, flag|SD_LEVEL|2);
    }
}
