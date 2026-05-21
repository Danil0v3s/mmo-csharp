using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_TEMPERING — Homunculus Tempering. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_tempering.cpp</c>.
/// Applies the tempering buff to master. Master lookup + SC enum are
/// TODO; we animate.
/// </summary>
public sealed class Tempering : SkillImpl
{
    public Tempering() : base(SkillIds.MH_TEMPERING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
