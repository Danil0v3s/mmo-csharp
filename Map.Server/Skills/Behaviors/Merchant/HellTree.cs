using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_HELLTREE — Biolo Hell Tree (bionic). Manual port of
/// <c>rathena-fork/src/map/skills/merchant/helltree.cpp</c>.
/// Same shape as <see cref="Creeper"/> — applies SC_BIONIC_HELLTREE.
/// </summary>
public sealed class HellTree : SkillImpl
{
    public HellTree() : base(SkillIds.BO_HELLTREE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.BionicHelltree, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
