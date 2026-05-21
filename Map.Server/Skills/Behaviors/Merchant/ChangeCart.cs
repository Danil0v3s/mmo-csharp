using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_CHANGECART — Merchant Change Cart. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/changecart.cpp</c>.
/// Opens the cart-skin chooser; the cast itself is a broadcast.
/// </summary>
public sealed class ChangeCart : SkillImpl
{
    public ChangeCart() : base(SkillIds.MC_CHANGECART) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
