using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_QD_SHOT — auto-generated stub from
/// <c>src/map/skills/gunslinger/quickdrawshot.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class QuickDrawShot : SkillImpl
{
    public QuickDrawShot() : base(SkillIds.RL_QD_SHOT) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, &src);
    // 
    // 	dmg.div_ = 1;
    // 	
    // 	if (sd != nullptr) {
    // 		dmg.div_ += sd->status.job_level / 20;
    // 	}
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Remember main target as it will always be hit by this skill
    // 	skill_area_temp[1] = target->id;
    // 	// Iterate through all enemies in the area
    // 	map_foreachinallrange(skill_area_sub, src, skill_get_splash(getSkillId(), skill_lv), BL_CHAR, src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | 1, skill_castend_damage_id);
    // 	// End here to prevent spamming of the skill onto the target
    // 	status_change_end(src, SC_QD_SHOT_READY);
    // 	skill_area_temp[1] = 0;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change* tsc = status_get_sc(target);
    // 
    // 	// Except for main target, only units marked with crimson marker are valid targets
    // 	if (skill_area_temp[1] == target->id || (tsc != nullptr && tsc->getSCE(SC_C_MARKER) != nullptr)) {
    // 		skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    // 	}
    }
}
