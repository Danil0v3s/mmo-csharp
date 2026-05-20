using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_CHARGEATK — auto-generated stub from
/// <c>src/map/skills/swordman/chargeattack.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ChargeAttack : SkillImpl
{
    public ChargeAttack() : base(SkillIds.KN_CHARGEATK) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // bool path = path_search_long(nullptr, src->m, src->x, src->y, target->x, target->y,CELL_CHKWALL);
    // #ifdef RENEWAL
    // 	int32 dist = skill_get_blewcount(getSkillId(), skill_lv);
    // #else
    // 	// Charge attack in pre-renewal calculates the distance mathetically
    // 	int32 dist = static_cast<int32>(distance_math_bl(src, target));
    // #endif
    // 	uint8 dir = map_calc_dir(target, src->x, src->y);
    // 
    // 	// teleport to target (if not on WoE grounds)
    // 	if (skill_check_unit_movepos(5, src, target->x + dirx[dir], target->y + diry[dir], 0, true))
    // 		clif_blown(src);
    // 
    // 	// cause damage and knockback if the path to target was a straight one
    // 	if (path) {
    // 		if(skill_attack(BF_WEAPON, src, src, target, getSkillId(), skill_lv, tick, dist)) {
    // #ifdef RENEWAL
    // 			if (map_getmapdata(src->m)->getMapFlag(MF_PVP))
    // 				dist += 2; // Knockback is 4 on PvP maps
    // #endif
    // 			skill_blown(src, target, dist, dir, BLOWN_NONE);
    // 		}
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	base_skillratio += 600;
    // #else
    // 	// +100% every 3 cells of distance but hard-limited to 500%
    // 	int32 k = (wd->miscflag - 1) / 3;
    // 	if (k < 0)
    // 		k = 0;
    // 	else if (k > 4)
    // 		k = 4;
    // 	base_skillratio += 100 * k;
    // #endif
    return baseRatio;
    }
}
