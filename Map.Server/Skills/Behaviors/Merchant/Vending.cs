using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_VENDING — Merchant Vending. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/vending.cpp</c>.
/// Pops the vending UI request (size = 2 + skillLevel) for the
/// caster's cart. Vending pipeline / cart persistence isn't ported
/// yet — animation only.
/// </summary>
public sealed class Vending : SkillImpl
{
    public Vending() : base(SkillIds.MC_VENDING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        // TODO: pc_can_give_items(sd) gate, intif_storage_save on cart pending, clif_openvendingreq(sd, 2 + skill_lv).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
