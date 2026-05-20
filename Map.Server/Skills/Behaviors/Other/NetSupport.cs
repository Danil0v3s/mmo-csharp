using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ABR_NET_SUPPORT — auto-generated stub from
/// <c>src/map/skills/other/netsupport.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class NetSupport : SkillImpl
{
    public NetSupport() : base(SkillIds.ABR_NET_SUPPORT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* tstatus = status_get_status_data(*target);
    // 
    // 	if (flag & 1) {
    // 		int32 heal_amount = tstatus->max_sp * 3 / 100;
    // 		clif_skill_nodamage(nullptr, *target, MG_SRECOVERY, heal_amount);
    // 		status_heal(target, 0, heal_amount, 0);
    // 	} else {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ALLY | SD_SPLASH | 1, skill_castend_nodamage_id);
    // 	}
    }
}
