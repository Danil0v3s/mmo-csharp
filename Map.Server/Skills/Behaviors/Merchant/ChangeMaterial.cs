using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_CHANGEMATERIAL — Genetic Change Material. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/changematerial.cpp</c>.
/// Opens the item-conversion list. UI packet (clif_skill_itemlistwindow)
/// not wired — broadcast only.
/// </summary>
public sealed class ChangeMaterial : SkillImpl
{
    public ChangeMaterial() : base(SkillIds.GN_CHANGEMATERIAL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
