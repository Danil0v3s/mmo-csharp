using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_CRESCIVE_BOLT — auto-generated stub from
/// <c>src/map/skills/archer/crescivebolt.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class CresciveBolt : WeaponSkillImpl
{
    public CresciveBolt() : base(SkillIds.WH_CRESCIVE_BOLT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 	const status_change *sc = status_get_sc(src);
    // 
    // 	skillratio += -100 + 500 + 1300 * skill_lv;
    // 	skillratio += 5 * sstatus->con;
    // 	RE_LVL_DMOD(100);
    // 	if (sc) {
    // 		if (sc->getSCE(SC_CRESCIVEBOLT))
    // 			skillratio += skillratio * (20 * sc->getSCE(SC_CRESCIVEBOLT)->val1) / 100;
    // 
    // 		if (sc->getSCE(SC_CALAMITYGALE)) {
    // 			skillratio += skillratio * 20 / 100;
    // 
    // 			if (tstatus->race == RC_BRUTE || tstatus->race == RC_FISH)
    // 				skillratio += skillratio * 50 / 100;
    // 		}
    // 	}
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 	if( sc && sc->getSCE(SC_CRESCIVEBOLT) )
    // 		sc_start(src, src, SC_CRESCIVEBOLT, 100, min( 3, 1 + sc->getSCE(SC_CRESCIVEBOLT)->val1 ), skill_get_time(getSkillId(), skill_lv));
    // 	else
    // 		sc_start(src, src, SC_CRESCIVEBOLT, 100, 1, skill_get_time(getSkillId(), skill_lv));
    }
}
