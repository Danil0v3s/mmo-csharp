using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_REVERBERATION — auto-generated stub from
/// <c>src/map/skills/archer/reverberation.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Reverberation : SkillImpl
{
    public Reverberation() : base(SkillIds.WM_REVERBERATION) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change_end(target, SC_SOUNDBLEND);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_change *tsc = status_get_sc(target);
    // 
    // 	// MATK [{(Skill Level x 300) + 400} x Casters Base Level / 100] %
    // 	skillratio += -100 + 700 + 300 * skill_lv;
    // 	RE_LVL_DMOD(100);
    // 	if (tsc && tsc->getSCE(SC_SOUNDBLEND))
    // 		skillratio += skillratio * 50 / 100;
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (flag & 1)
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 	else {
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		map_foreachinallrange(skill_area_sub, target, skill_get_splash(getSkillId(), skill_lv), BL_CHAR|BL_SKILL, src, getSkillId(), skill_lv, tick, flag|BCT_ENEMY|SD_SPLASH|1, skill_castend_damage_id);
    // 		battle_consume_ammo(sd, getSkillId(), skill_lv); // Consume here since Magic/Misc attacks reset arrow_atk
    // 	}
    }
}
