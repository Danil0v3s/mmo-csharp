using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_REST — Vaporize (Homunculus rest). Manual port of
/// <c>rathena-fork/src/map/skills/merchant/vaporize.cpp</c>.
/// Calls hom_vaporize(HOM_ST_REST). Homunculus service not yet wired
/// — animation only.
/// </summary>
public sealed class Vaporize : SkillImpl
{
    public Vaporize() : base(SkillIds.AM_REST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: hom_vaporize(sd, HOM_ST_REST) → on success animate, else SkillFailCause.Level.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
