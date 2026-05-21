using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_PHARMACY — Alchemist Prepare Potion. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/preparepotion.cpp</c>.
/// Opens the produce-mix list. UI packet TODO.
/// </summary>
public sealed class PreparePotion : SkillImpl
{
    public PreparePotion() : base(SkillIds.AM_PHARMACY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
