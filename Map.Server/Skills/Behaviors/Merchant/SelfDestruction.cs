using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_SELFDESTRUCTION — auto-generated stub from
/// <c>src/map/skills/merchant/selfdestruction.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SelfDestruction : RecursiveDamageSplashSkillImpl
{
    public SelfDestruction() : base(SkillIds.NC_SELFDESTRUCTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd == nullptr) {
    // 		return;
    // 	}
    // 
    // 	if (pc_ismadogear(sd)) {
    // 		pc_setmadogear(sd, false);
    // 	}
    // 
    // 	skill_area_temp[1] = 0;
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	map_foreachinrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR | BL_SKILL, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | SD_SPLASH | 1, skill_castend_damage_id);
    // 	status_set_sp(src, 0, 0);
    // 	skill_clear_unitgroup(src);
    }
}
