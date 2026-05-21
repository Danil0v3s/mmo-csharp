using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_STRIPSHIELD — Divest Shield. Manual port of
/// <c>rathena-fork/src/map/skills/thief/divestshield.cpp</c>.
/// Strips the target's shield. Strip service is TODO.
/// </summary>
public sealed class DivestShield : SkillImpl
{
    public DivestShield() : base(SkillIds.RG_STRIPSHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
