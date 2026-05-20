using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_STORMGUST — auto-generated stub from
/// <c>src/map/skills/mage/stormgust.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class StormGust : SkillImpl
{
    public StormGust() : base(SkillIds.WZ_STORMGUST) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	flag |= 1;
    // 
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // #ifdef RENEWAL
    // 	base_skillratio -= 30; // Offset only once
    // 	base_skillratio += 50 * skill_lv;
    // #else
    // 	base_skillratio += 40 * skill_lv;
    // #endif
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // Storm Gust counter was dropped in renewal
    // #ifdef RENEWAL
    // 	sc_start(src,target,SC_FREEZE,65-(5*skill_lv),skill_lv,skill_get_time2(getSkillId(),skill_lv));
    // #else
    // 	status_change* tsc = status_get_sc( target );
    // 
    // 	if (tsc != nullptr) {
    // 		//On third hit, there is a 150% to freeze the target
    // 		if(tsc->sg_counter >= 3 &&
    // 			sc_start(src,target,SC_FREEZE,150,skill_lv,skill_get_time2(getSkillId(),skill_lv)))
    // 			tsc->sg_counter = 0;
    // 		// Being it only resets on success it'd keep stacking and eventually overflowing on mvps, so we reset at a high value
    // 		else if( tsc->sg_counter > 250 )
    // 			tsc->sg_counter = 0;
    // 	}
    // #endif
    }
}
