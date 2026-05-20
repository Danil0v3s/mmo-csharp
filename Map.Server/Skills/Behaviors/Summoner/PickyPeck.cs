using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_PICKYPECK — auto-generated stub from
/// <c>src/map/skills/summoner/pickypeck.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class PickyPeck : WeaponSkillImpl
{
    public PickyPeck() : base(SkillIds.SU_PICKYPECK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	base_skillratio += 100 + 100 * skill_lv;
    // 	if (status_get_hp(target) < (status_get_max_hp(target) / 2))
    // 		base_skillratio *= 2;
    // 	if (sd && pc_checkskill(sd, SU_SPIRITOFLIFE))
    // 		base_skillratio += base_skillratio * status_get_hp(src) / status_get_max_hp(src);
    return baseRatio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 
    // 	WeaponSkillImpl::castendDamageId(src, target, skill_lv, tick, flag);
    }


}
