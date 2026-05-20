using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_HAWKBOOMERANG — auto-generated stub from
/// <c>src/map/skills/archer/hawkboomerang.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HawkBoomerang : WeaponSkillImpl
{
    public HawkBoomerang() : base(SkillIds.WH_HAWKBOOMERANG) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	skillratio += -100 + 600 * skill_lv + 10 * sstatus->con;
    // 	if (sd)
    // 		skillratio += skillratio * pc_checkskill(sd, WH_NATUREFRIENDLY) / 10;
    // 	if (tstatus->race == RC_BRUTE || tstatus->race == RC_FISH)
    // 		skillratio += skillratio * 50 / 100;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    }
}
