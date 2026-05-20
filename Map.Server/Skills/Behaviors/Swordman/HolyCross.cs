using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_HOLYCROSS — auto-generated stub from
/// <c>src/map/skills/swordman/holycross.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class HolyCross : WeaponSkillImpl
{
    public HolyCross() : base(SkillIds.CR_HOLYCROSS) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if(sd && sd->status.weapon == W_2HSPEAR)
    // 		base_skillratio += 70 * skill_lv;
    // 	else
    // #endif
    // 		base_skillratio += 35 * skill_lv;
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start(src,target,SC_BLIND,3*skill_lv,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    }
}
