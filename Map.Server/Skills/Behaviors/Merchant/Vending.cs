using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_VENDING — auto-generated stub from
/// <c>src/map/skills/merchant/skill_vending.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Vending : SkillImpl
{
    public Vending() : base(SkillIds.MC_VENDING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data *sd = BL_CAST(BL_PC, src);
    // 	if (sd) {
    // 		// Prevent vending of GMs with unnecessary Level to trade/drop. [Skotlex]
    // 		if (!pc_can_give_items(sd))
    // 			clif_skill_fail(*sd, MC_VENDING);
    // 		else {
    // 			int32 i = 0;
    // 			sd->state.prevend = 1;
    // 			sd->state.workinprogress = WIP_DISABLE_ALL;
    // 			sd->vend_skill_lv = skill_lv;
    // 			ARR_FIND(0, MAX_CART, i, sd->cart.u.items_cart[i].nameid && sd->cart.u.items_cart[i].id == 0);
    // 			if (i < MAX_CART) {
    // 				// Save the cart before opening the vending UI
    // 				sd->state.pending_vending_ui = true;
    // 				intif_storage_save(sd, &sd->cart);
    // 			} else {
    // 				// Instantly open the vending UI
    // 				sd->state.pending_vending_ui = false;
    // 				clif_openvendingreq(*sd, 2 + skill_lv);
    // 			}
    // 		}
    // 	}
    }
}
