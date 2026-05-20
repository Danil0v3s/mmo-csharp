using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_FLAMELAUNCHER — auto-generated stub from
/// <c>src/map/skills/merchant/flamelauncher.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FlameLauncher : SkillImpl
{
    public FlameLauncher() : base(SkillIds.NC_FLAMELAUNCHER) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, SC_BURNING, 20 + 10 * skill_lv, skill_lv, skill_get_time(getSkillId(), skill_lv));
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skillratio += 200 + 300 * skill_lv;
    // 	RE_LVL_DMOD(150);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_area_temp[1] = target->id;
    // 	if (battle_config.skill_eightpath_algorithm) {
    // 		//Use official AoE algorithm
    // 		map_foreachindir(skill_attack_area, src->m, src->x, src->y, target->x, target->y,
    // 			skill_get_splash(getSkillId(), skill_lv), skill_get_maxcount(getSkillId(), skill_lv), 0, splash_target(src),
    // 			skill_get_type(getSkillId()), src, src, getSkillId(), skill_lv, tick, flag, BCT_ENEMY);
    // 	} else {
    // 		map_foreachinpath(skill_attack_area, src->m, src->x, src->y, target->x, target->y,
    // 			skill_get_splash(getSkillId(), skill_lv), skill_get_maxcount(getSkillId(), skill_lv), splash_target(src),
    // 			skill_get_type(getSkillId()), src, src, getSkillId(), skill_lv, tick, flag, BCT_ENEMY);
    // 	}
    }
}
