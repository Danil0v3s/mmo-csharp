using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_STRIPHELM — Divest Helm. Manual port of
/// <c>rathena-fork/src/map/skills/thief/divesthelm.cpp</c>.
/// Strips the target's helm. Strip service is TODO.
/// </summary>
public sealed class DivestHelm : SkillImpl
{
    public DivestHelm() : base(SkillIds.RG_STRIPHELM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
