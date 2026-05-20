using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// PF_HPCONVERSION — auto-generated stub from
/// <c>src/map/skills/mage/indulge.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Indulge : SkillImpl
{
    public Indulge() : base(SkillIds.PF_HPCONVERSION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const status_data* sstatus = status_get_status_data(*src);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 	int32 hp = sstatus->max_hp / 10;
    // 	int32 sp = hp * skill_lv;
    // 
    // 	if (!status_charge(src, hp, 0)) {
    // 		if (sd != nullptr) {
    // 			clif_skill_fail(*sd, getSkillId());
    // 		}
    // 		return;
    // 	}
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	status_heal(target, 0, sp, 2);
    }
}
