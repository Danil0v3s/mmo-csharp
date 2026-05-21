using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_SELFDESTRUCTION — Mechanic Self Destruction. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/selfdestruction.cpp</c>.
/// Drains all SP and splash-damages enemies. Madogear-off toggle +
/// SP zap TODO; we broadcast only.
/// </summary>
public sealed class SelfDestruction : RecursiveDamageSplashSkillImpl
{
    public SelfDestruction() : base(SkillIds.NC_SELFDESTRUCTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
