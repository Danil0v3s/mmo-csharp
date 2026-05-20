using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_ACTIVITY_BURN — auto-generated stub from
/// <c>src/map/skills/mage/activityburn.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ActivityBurn : SkillImpl
{
    public ActivityBurn() : base(SkillIds.EM_ACTIVITY_BURN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	if (target->type == BL_PC && rnd() % 100 < 20 + 10 * skill_lv) {
    // 		uint8 ap_burn[5] = { 20, 30, 50, 60, 70 };
    // 
    // 		clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		status_zap(target, 0, 0, ap_burn[skill_lv - 1]);
    // 	} else if (sd)
    // 		clif_skill_fail( *sd, getSkillId(), USESKILL_FAIL );
    }
}
