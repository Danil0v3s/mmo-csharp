using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_FINGEROFFENSIVE — auto-generated stub from
/// <c>src/map/skills/acolyte/throwspiritsphere.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ThrowSpiritSphere : WeaponSkillImpl
{
    public ThrowSpiritSphere() : base(SkillIds.MO_FINGEROFFENSIVE) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, &src);
    // 
    // 	if (sd != nullptr) {
    // 		if (battle_config.finger_offensive_type)
    // 			dmg.div_ = 1;
    // #ifndef RENEWAL
    // 		else if ((sd->spiritball + sd->spiritball_old) < dmg.div_)
    // 			dmg.div_ = sd->spiritball + sd->spiritball_old;
    // #endif
    // 	}
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    // 	if (battle_config.finger_offensive_type && sd) {
    // 		for (int32 i = 1; i < sd->spiritball_old; i++)
    // 			skill_addtimerskill(src, tick + i * 200, target->id, 0, 0, getSkillId(), skill_lv, BF_WEAPON, flag);
    // 	}
    // 	status_change_end(src, SC_BLADESTOP);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const status_change* tsc = status_get_sc(target);
    // 
    // 	base_skillratio += 500 + skill_lv * 200;
    // 	if (tsc && tsc->getSCE(SC_BLADESTOP))
    // 		base_skillratio += base_skillratio / 2;
    // #else
    // 	base_skillratio += 50 * skill_lv;
    // #endif
    return baseRatio;
    }
}
