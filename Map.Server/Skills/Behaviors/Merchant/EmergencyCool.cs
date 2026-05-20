using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_EMERGENCYCOOL — auto-generated stub from
/// <c>src/map/skills/merchant/emergencycool.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class EmergencyCool : SkillImpl
{
    public EmergencyCool() : base(SkillIds.NC_EMERGENCYCOOL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 	if (sd == nullptr) {
    // 		return;
    // 	}
    // 
    // 	struct s_skill_condition req = skill_get_requirement(sd, getSkillId(), skill_lv);
    // 	int16 limit[] = { -45, -75, -105 };
    // 	int32 i = 0;
    // 
    // 	for (const auto& reqItem : req.eqItem) {
    // 		if (pc_search_inventory(sd, reqItem) != -1) {
    // 			break;
    // 		}
    // 		i++;
    // 	}
    // 
    // 	pc_overheat(*sd, limit[(i > 2) ? 2 : i]);
    }
}
