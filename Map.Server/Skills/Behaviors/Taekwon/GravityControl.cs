using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// SJ_GRAVITYCONTROL — auto-generated stub from
/// <c>src/map/skills/taekwon/gravitycontrol.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class GravityControl : SkillImpl
{
    public GravityControl() : base(SkillIds.SJ_GRAVITYCONTROL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_data* sstatus = status_get_status_data(*src);
    // 	status_data* tstatus = status_get_status_data(*target);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	int32 fall_damage = sstatus->batk + sstatus->rhw.atk - tstatus->def2;
    // 
    // 	if (target->type == BL_PC)
    // 		fall_damage += dstsd->weight / 10 - tstatus->def;
    // 	else // Monster's don't have weight. Put something in its place.
    // 		fall_damage += 50 * status_get_lv(src) - tstatus->def;
    // 
    // 	fall_damage = max(1, fall_damage);
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv, sc_start2(src, target, skill_get_sc(getSkillId()), 100, skill_lv, fall_damage, skill_get_time(getSkillId(), skill_lv)));
    }
}
