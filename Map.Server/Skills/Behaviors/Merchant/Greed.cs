using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_GREED — Blacksmith Greed. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/greed.cpp</c>.
/// Auto-loots floor items in range. skill_greed iteration TODO.
/// </summary>
public sealed class Greed : SkillImpl
{
    public Greed() : base(SkillIds.BS_GREED) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: iterate BL_ITEM in splash and auto-pickup.
    }
}
