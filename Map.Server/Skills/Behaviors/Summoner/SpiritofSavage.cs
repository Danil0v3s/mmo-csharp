using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_SVG_SPIRIT — auto-generated stub from
/// <c>src/map/skills/summoner/spiritofsavage.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SpiritofSavage : SkillImpl
{
    public SpiritofSavage() : base(SkillIds.SU_SVG_SPIRIT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	base_skillratio += 150 + 150 * skill_lv;
    // 	if (sd && pc_checkskill(sd, SU_SPIRITOFLIFE))
    // 		base_skillratio += base_skillratio * status_get_hp(src) / status_get_max_hp(src);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_area_temp[1] = target->id;
    // 	map_foreachinpath(skill_attack_area, src->m, src->x, src->y, target->x, target->y,
    // 		skill_get_splash(getSkillId(), skill_lv), skill_get_maxcount(getSkillId(), skill_lv), splash_target(src),
    // 		skill_get_type(getSkillId()), src, src, getSkillId(), skill_lv, tick, flag, BCT_ENEMY);
    }
}
