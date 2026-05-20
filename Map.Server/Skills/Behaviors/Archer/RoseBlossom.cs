using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_ROSEBLOSSOM — auto-generated stub from
/// <c>src/map/skills/archer/roseblossom.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class RoseBlossom : WeaponSkillImpl
{
    public RoseBlossom() : base(SkillIds.TR_ROSEBLOSSOM) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_change* sc = status_get_sc(src);
    // 	const status_change* tsc = status_get_sc(target);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 	const status_data* tstatus = status_get_status_data(*target);
    // 
    // 	skillratio += -100 + 200 + 2000 * skill_lv;
    // 
    // 	if (sd && pc_checkskill(sd, TR_STAGE_MANNER) > 0)
    // 		skillratio += 3 * sstatus->con;
    // 
    // 	if( tsc != nullptr && tsc->getSCE( SC_SOUNDBLEND ) ){
    // 		skillratio += 200 * skill_lv;
    // 	}
    // 
    // 	RE_LVL_DMOD(100);
    // 	if (sc && sc->getSCE(SC_MYSTIC_SYMPHONY)) {
    // 		skillratio *= 2;
    // 
    // 		if (tstatus->race == RC_FISH || tstatus->race == RC_DEMIHUMAN)
    // 			skillratio += skillratio * 50 / 100;
    // 	}
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Rose blossom seed can only bloom if the target is hit.
    // 	sc_start4(src, target, SC_ROSEBLOSSOM, 100, skill_lv, TR_ROSEBLOSSOM_ATK, src->id, 0, skill_get_time(getSkillId(), skill_lv));
    // 	status_change_end(target, SC_SOUNDBLEND);
    }

}
