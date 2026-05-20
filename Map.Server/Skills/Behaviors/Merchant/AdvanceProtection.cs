using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_ADVANCE_PROTECTION — auto-generated stub from
/// <c>src/map/skills/merchant/advanceprotection.hpp</c>.
///
/// <para>Inherits <see cref="StatusSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class AdvanceProtection : StatusSkillImpl
{
    public AdvanceProtection() : base(SkillIds.BO_ADVANCE_PROTECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if (dstsd == nullptr || pc_checkequip(dstsd, EQP_SHADOW_GEAR) < 0) {
    // 		if (map_session_data* sd = BL_CAST(BL_PC, src); sd != nullptr) {
    // 			clif_skill_fail(*sd, getSkillId());
    // 
    // 		}
    // 
    // 		// Don't consume item requirements
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 
    // 	StatusSkillImpl::castendNoDamageId(src, target, skill_lv, tick, flag);
    }
}
