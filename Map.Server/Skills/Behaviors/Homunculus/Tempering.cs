using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_TEMPERING — Homunculus Tempering. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_tempering.cpp</c>.
/// Applies the tempering buff to master. Master-lookup + dedicated
/// SC enum are deferred per PARITY-REMAINING.md §P2 (homunculus
/// master-link plumbing not yet on HomunculusEntity); the cast frame
/// animates correctly today.
/// </summary>
public sealed class Tempering : SkillImpl
{
    public Tempering() : base(SkillIds.MH_TEMPERING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
