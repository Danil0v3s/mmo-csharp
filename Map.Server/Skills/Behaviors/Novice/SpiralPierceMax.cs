using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_SPIRAL_PIERCE_MAX — auto-generated stub from
/// <c>src/map/skills/novice/spiralpiercemax.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpiralPierceMax : WeaponSkillImpl
{
    public SpiralPierceMax() : base(SkillIds.HN_SPIRAL_PIERCE_MAX) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* dstsd = BL_CAST( BL_PC, target );
    // 	mob_data *dstmd = BL_CAST(BL_MOB, target);
    // 
    // 	if( dstsd || ( dstmd && !status_bl_has_mode(target,MD_STATUSIMMUNE) ) ) //Does not work on status immune
    // 		sc_start(src,target,SC_ANKLE,100,0,skill_get_time2(getSkillId(),skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	const map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	skillratio += -100 + 1000 + 1500 * skill_lv;
    // 	skillratio += pc_checkskill(sd, HN_SELFSTUDY_TATICS) * 3 * skill_lv;
    // 	skillratio += 5 * sstatus->pow;
    // 	switch (status_get_size(target)){
    // 		case SZ_SMALL:
    // 			skillratio = skillratio * 150 / 100;
    // 			break;
    // 		case SZ_MEDIUM:
    // 			skillratio = skillratio * 130 / 100;
    // 			break;
    // 		case SZ_BIG:
    // 			skillratio = skillratio * 120 / 100;
    // 			break;
    // 	}
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
