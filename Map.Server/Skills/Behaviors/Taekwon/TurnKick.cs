using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_TURNKICK — auto-generated stub from
/// <c>src/map/skills/taekwon/turnkick.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class TurnKick : SkillImpl
{
    public TurnKick() : base(SkillIds.TK_TURNKICK) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // dmg.blewcount = 0;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // base_skillratio += 90 + 30 * skill_lv;
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Note: attack_type is passed as BF_WEAPON for the actual target, BF_MISC for the splash-affected mobs.
    // 	if (attack_type & BF_MISC) {
    // 		sc_start(src, target, SC_STUN, 200, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 		clif_specialeffect(target, EF_SPINEDBODY, AREA);
    // 		sc_start(src, target, SC_NOACTION, 100, 1, skill_get_time2(getSkillId(), skill_lv));
    // 	}
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Active part of the attack.
    // 	// Note: skill_area_temp[1] is used in castendNoDamageId to avoid affecting the target.
    // 	skill_area_temp[1] = target->id;
    // 
    // 	if (skill_attack(BF_WEAPON, src, src, target, getSkillId(), skill_lv, tick, flag))
    // 		map_foreachinallrange(skill_area_sub, target,
    // 		                      skill_get_splash(getSkillId(), skill_lv), BL_MOB,
    // 		                      src, getSkillId(), skill_lv, tick, flag | BCT_ENEMY | 1,
    // 		                      skill_castend_nodamage_id);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Passive part of the attack. Splash knock-back+stun.
    // 	if (skill_area_temp[1] != target->id) {
    // 		skill_blown(src, target, skill_get_blewcount(getSkillId(), skill_lv), -1, BLOWN_NONE);
    // 		skill_additional_effect(src, target, getSkillId(), skill_lv, BF_MISC, ATK_DEF, tick); // Use Misc rather than weapon to signal passive pushback
    // 	}
    }
}
