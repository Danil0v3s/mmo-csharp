using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_ADRENALINE2 — auto-generated stub from
/// <c>src/map/skills/merchant/advancedadrenalinerush.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AdvancedAdrenalineRush : SkillImpl
{
    public AdvancedAdrenalineRush() : base(SkillIds.BS_ADRENALINE2) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if (sd == nullptr || sd->status.party_id == 0 || (flag & 1)) {
    // 		int32 weapontype = skill_get_weapontype(getSkillId());
    // 		if (!weapontype || !dstsd || pc_check_weapontype(dstsd, weapontype)) {
    // 			clif_skill_nodamage(target, *target, getSkillId(), skill_lv,
    // 				sc_start2(src, target, skill_get_sc(getSkillId()), 100, skill_lv, (src == target) ? 1 : 0, skill_get_time(getSkillId(), skill_lv)));
    // 		}
    // 	} else if (sd) {
    // 		party_foreachsamemap(skill_area_sub,
    // 			sd,skill_get_splash(getSkillId(), skill_lv),
    // 			src,getSkillId(),skill_lv,tick, flag|BCT_PARTY|1,
    // 			skill_castend_nodamage_id);
    // 	}
    }
}
