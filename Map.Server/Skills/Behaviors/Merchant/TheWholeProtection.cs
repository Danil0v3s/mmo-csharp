using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_THE_WHOLE_PROTECTION — auto-generated stub from
/// <c>src/map/skills/merchant/thewholeprotection.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class TheWholeProtection : SkillImpl
{
    public TheWholeProtection() : base(SkillIds.BO_THE_WHOLE_PROTECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if (sd == nullptr || sd->status.party_id == 0 || (flag & 1)) {
    // 		uint32 equip[] = { EQP_WEAPON, EQP_SHIELD, EQP_ARMOR, EQP_HEAD_TOP };
    // 
    // 		for (uint8 i_eqp = 0; i_eqp < 4; i_eqp++) {
    // 			if (target->type != BL_PC || (dstsd && pc_checkequip(dstsd, equip[i_eqp]) < 0))
    // 				continue;
    // 			sc_start(src, target, (sc_type)(SC_CP_WEAPON + i_eqp), 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 		}
    // 	} else if (sd) {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		party_foreachsamemap(skill_area_sub, sd, skill_get_splash(getSkillId(), skill_lv), src, getSkillId(), skill_lv, tick, flag | BCT_PARTY | 1, skill_castend_nodamage_id);
    // 	}
    }
}
