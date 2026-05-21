using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_STRIP_SHADOW — Strip Shadow. Manual port of
/// <c>rathena-fork/src/map/skills/thief/stripshadow.cpp</c>.
/// Strips shadow gear from the target. Strip service is TODO.
/// </summary>
public sealed class StripShadow : SkillImpl
{
    public StripShadow() : base(SkillIds.ABC_STRIP_SHADOW) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
