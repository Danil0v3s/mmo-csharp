using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_ACIDTERROR — auto-generated stub from
/// <c>src/map/skills/merchant/acidterror.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AcidTerror : WeaponSkillImpl
{
    public AcidTerror() : base(SkillIds.AM_ACIDTERROR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	base_skillratio += -100 + 200 * skill_lv;
    // 	if (sd && pc_checkskill(sd, AM_LEARNINGPOTION))
    // 		base_skillratio += 100; // !TODO: What's this bonus increase?
    // #else
    // 	base_skillratio += -50 + 50 * skill_lv;
    // #endif
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // sc_start2(src,target,SC_BLEEDING,(skill_lv*3),skill_lv,src->id,skill_get_time2(getSkillId(),skill_lv));
    // #ifdef RENEWAL
    // 	if (skill_break_equip(src,target, EQP_ARMOR, (1000 * skill_lv + 500) - 1000, BCT_ENEMY))
    // #else
    // 	if (skill_break_equip(src,target, EQP_ARMOR, 100*skill_get_time(getSkillId(),skill_lv), BCT_ENEMY))
    // #endif
    // 		clif_emotion( *target, ET_HUK );
    }
}
