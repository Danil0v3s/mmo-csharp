using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SUMMONFB — auto-generated stub from
/// <c>src/map/skills/mage/summonfireball.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class SummonFireBall : SkillImpl
{
    public SummonFireBall() : base(SkillIds.WL_SUMMONFB) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change *sc = status_get_sc(src);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 	int32 i = 0;
    // 
    // 	if (sc == nullptr)
    // 		return;
    // 
    // 	// Set val2. The SC element for this ball
    // 	e_wl_spheres element = WLS_FIRE;
    // 
    // 	if (skill_lv == 1) {
    // 		sc_type sphere = SC_NONE;
    // 
    // 		for (i = SC_SPHERE_1; i <= SC_SPHERE_5; i++) {
    // 			if (sc->getSCE(i) == nullptr) {
    // 				sphere = static_cast<sc_type>(i); // Take the free SC
    // 				break;
    // 			}
    // 		}
    // 
    // 		if (sphere == SC_NONE) {
    // 			if (sd) // No free slots to put SC
    // 				clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL_SUMMON );
    // 			return;
    // 		}
    // 
    // 		sc_start2(src, src, sphere, 100, element, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 	} else {
    // 		for (i = SC_SPHERE_1; i <= SC_SPHERE_5; i++) {
    // 			status_change_end(src, static_cast<sc_type>(i)); // Removes previous type
    // 			sc_start2(src, src, static_cast<sc_type>(i), 100, element, skill_lv, skill_get_time(getSkillId(), skill_lv));
    // 		}
    // 	}
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), 0, false);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    //     // (empty / no-op in rAthena)
    return baseRatio;
    }
}
