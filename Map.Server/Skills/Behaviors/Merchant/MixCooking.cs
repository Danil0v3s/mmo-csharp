using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_MIX_COOKING — Genetic Mix Cooking. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/mixcooking.cpp</c>.
/// Opens the Genetic cooking panel.
/// </summary>
public sealed class MixCooking : SkillImpl
{
    public MixCooking() : base(SkillIds.GN_MIX_COOKING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
