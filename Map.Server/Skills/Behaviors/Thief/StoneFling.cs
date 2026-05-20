using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_THROWSTONE — auto-generated stub from
/// <c>src/map/skills/thief/stonefling.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class StoneFling : SkillImpl
{
    public StoneFling() : base(SkillIds.TF_THROWSTONE) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data *sd = BL_CAST(BL_PC, src);
    // 	if (sd != nullptr) {
    // 		// Only blind if used by player and stun failed
    // 		if (!sc_start(src, target, SC_STUN, 3, skill_lv, skill_get_time(getSkillId(), skill_lv)))
    // 			sc_start(src, target, SC_BLIND, 3, skill_lv, skill_get_time2(getSkillId(), skill_lv));
    // 	} else {
    // 		// 5% stun chance and no blind chance when used by monsters
    // 		sc_start(src, target, SC_STUN, 5, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	}
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skill_attack(skill_get_type(getSkillId()), src, src, target, getSkillId(), skill_lv, tick, flag);
    }
}
