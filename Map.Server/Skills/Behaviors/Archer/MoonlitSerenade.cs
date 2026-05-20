using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WA_MOONLIT_SERENADE — auto-generated stub from
/// <c>src/map/skills/archer/moonlitserenade.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MoonlitSerenade : SkillImpl
{
    public MoonlitSerenade() : base(SkillIds.WA_MOONLIT_SERENADE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	sc_type type = skill_get_sc(getSkillId());
    // 
    // 	if (sd == nullptr || sd->status.party_id == 0 || (flag & 1)) {
    // 		clif_skill_nodamage(src, *bl, getSkillId(), skill_lv);
    // 		sc_start2(src, bl, type, 100, skill_lv, ((sd) ? pc_checkskill(sd, WM_LESSON) : skill_get_max(WM_LESSON)), skill_get_time(getSkillId(), skill_lv));
    // 	} else if (sd) {
    // 		party_foreachsamemap(skill_area_sub, sd, skill_get_splash(getSkillId(), skill_lv), src, getSkillId(), skill_lv, tick, flag | BCT_PARTY | 1, skill_castend_nodamage_id);
    // 		sc_start2(src, bl, type, 100, skill_lv, ((sd) ? pc_checkskill(sd, WM_LESSON) : skill_get_max(WM_LESSON)), skill_get_time(getSkillId(), skill_lv));
    // 		clif_skill_nodamage(src, *bl, getSkillId(), skill_lv);
    // 	}
    }
}
