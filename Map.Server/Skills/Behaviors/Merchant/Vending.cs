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
        // Deferred: full vending bring-up needs pc_can_give_items (trade-gate check),
        // intif_storage_save (cart sync via the char/inter server), and the
        // clif_openvendingreq packet — none of these are ported yet.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
