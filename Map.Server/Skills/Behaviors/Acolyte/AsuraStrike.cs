using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_EXTREMITYFIST — auto-generated stub from
/// <c>src/map/skills/acolyte/asurastrike.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AsuraStrike : WeaponSkillImpl
{
    public AsuraStrike() : base(SkillIds.MO_EXTREMITYFIST) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // int16 x, y, i = 3; // Move 3 cells (From caster)
    // 	int16 dir = map_calc_dir(src,target->x,target->y);
    // 
    // #ifdef RENEWAL
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd && sd->spiritball_old > 5)
    // 		flag |= 1; // Give +100% damage increase
    // #endif
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 
    // 	status_set_sp(src, 0, 0);
    // 	sc_start(src, src, SC_EXTREMITYFIST, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	status_change_end(src, SC_EXPLOSIONSPIRITS);
    // 	status_change_end(src, SC_BLADESTOP);
    // 
    // 	if (dir > 0 && dir < 4)
    // 		x = -i;
    // 	else if (dir > 4)
    // 		x = i;
    // 	else
    // 		x = 0;
    // 	if (dir > 2 && dir < 6)
    // 		y = -i;
    // 	else if (dir == 7 || dir < 2)
    // 		y = i;
    // 	else
    // 		y = 0;
    // 
    // 	if (unit_movepos(src, src->x + x, src->y + y, 1, 1)) {
    // 		clif_blown(src);
    // 		clif_spiritball(src);
    // 	}
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	base_skillratio += 700 + sstatus->sp * 10;
    // #ifdef RENEWAL
    // 	if (wd->miscflag&1)
    // 		base_skillratio *= 2; // More than 5 spirit balls active
    // #endif
    // 	base_skillratio = min(500000,base_skillratio); //We stop at roughly 50k SP for overflow protection
    return baseRatio;
    }
}
