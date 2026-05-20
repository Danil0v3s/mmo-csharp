using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_MUSICAL_INTERLUDE — auto-generated stub from
/// <c>src/map/skills/archer/musicalinterlude.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MusicalInterlude : SkillImpl
{
    public MusicalInterlude() : base(SkillIds.TR_MUSICAL_INTERLUDE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (sd == nullptr || sd->status.party_id == 0 || (flag & 1))
    // 		sc_start4(src, bl, skill_get_sc(getSkillId()), 100, skill_lv, 0, flag, 0, skill_get_time(getSkillId(), skill_lv));
    // 	else if (sd) {
    // 		clif_skill_nodamage(bl, *bl, getSkillId(), skill_lv);
    // 
    // 		sd->skill_id_song = getSkillId();
    // 		sd->skill_lv_song = skill_lv;
    // 
    // 		if (skill_check_pc_partner(sd, getSkillId(), &skill_lv, AREA_SIZE, 0) > 0)
    // 			flag |= 2;
    // 
    // 		party_foreachsamemap(skill_area_sub, sd, skill_get_splash(getSkillId(), skill_lv), src, getSkillId(), skill_lv, tick, flag | BCT_PARTY | 1, skill_castend_nodamage_id);
    // 	}
    }
}
