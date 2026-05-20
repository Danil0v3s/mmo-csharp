using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_REPAIR — auto-generated stub from
/// <c>src/map/skills/merchant/repair.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Repair : SkillImpl
{
    public Repair() : base(SkillIds.NC_REPAIR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if (sd == nullptr) {
    // 		return;
    // 	}
    // 
    // 	if (!dstsd || !pc_ismadogear(dstsd)) {
    // 		clif_skill_fail(*sd, getSkillId(), USESKILL_FAIL_TOTARGET);
    // 		return;
    // 	}
    // 
    // 	int32 hp = 0;
    // 	switch (skill_lv) {
    // 		case 1: hp = 4; break;
    // 		case 2: hp = 7; break;
    // 		case 3: hp = 13; break;
    // 		case 4: hp = 17; break;
    // 		case 5:
    // 		default: hp = 23; break;
    // 	}
    // 
    // 	int32 heal = dstsd->status.max_hp * hp / 100;
    // 	status_heal(target, heal, 0, 2);
    // 	clif_skill_nodamage(src, *target, getSkillId(), skill_lv, heal != 0);
    }
}
