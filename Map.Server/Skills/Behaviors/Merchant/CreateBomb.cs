using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_MAKEBOMB — Genetic Create Bomb. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/createbomb.cpp</c>.
/// Opens the bomb-crafting list. UI packet not wired — broadcast only.
/// </summary>
public sealed class CreateBomb : SkillImpl
{
    public CreateBomb() : base(SkillIds.GN_MAKEBOMB) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
