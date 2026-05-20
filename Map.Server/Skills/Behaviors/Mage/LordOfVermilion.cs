using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_VERMILION — auto-generated stub from
/// <c>src/map/skills/mage/lordofvermilion.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class LordOfVermilion : SkillImpl
{
    public LordOfVermilion() : base(SkillIds.WZ_VERMILION) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	flag |= 1;
    // 
    // 	skill_unitsetting(src, getSkillId(),skill_lv,x,y,0);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	const map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if(sd)
    // 		base_skillratio += 300 + skill_lv * 100;
    // 	else
    // 		base_skillratio += 20 * skill_lv - 20; //Monsters use old formula
    // #else
    // 	base_skillratio += 20 * skill_lv - 20;
    // #endif
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	sc_start(src,target,SC_BLIND,10 + 5 * skill_lv,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // #else
    // 	sc_start(src,target,SC_BLIND,min(4*skill_lv,40),skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // #endif
    }
}
