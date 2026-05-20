using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_SCAROFTAROU — auto-generated stub from
/// <c>src/map/skills/summoner/scaroftarou.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ScarofTarou : WeaponSkillImpl
{
    public ScarofTarou() : base(SkillIds.SU_SCAROFTAROU) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, SC_STUN, 10, skill_lv, skill_get_time2(getSkillId(), skill_lv)); //! TODO: What's the chance/time?
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	base_skillratio += -100 + 100 * skill_lv;
    // 	if (sd && pc_checkskill(sd, SU_SPIRITOFLIFE))
    // 		base_skillratio += base_skillratio * status_get_hp(src) / status_get_max_hp(src);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src, target, SC_BITESCAR, 10, skill_lv, skill_get_time(getSkillId(), skill_lv)); //! TODO: What's the activation chance for the Bite effect?
    // 
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    }
}
