using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_CARTDECORATE — Merchant Decorate Cart. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/decoratecart.cpp</c>.
/// Opens cart-skin selector. UI packet (clif_SelectCart) not wired.
/// </summary>
public sealed class DecorateCart : SkillImpl
{
    public DecorateCart() : base(SkillIds.MC_CARTDECORATE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
