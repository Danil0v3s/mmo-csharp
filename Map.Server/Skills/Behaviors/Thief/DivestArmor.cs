using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_STRIPARMOR — Divest Armor. Manual port of
/// <c>rathena-fork/src/map/skills/thief/divestarmor.cpp</c>.
/// Strips the target's armor. Strip service is TODO.
/// </summary>
public sealed class DivestArmor : SkillImpl
{
    public DivestArmor() : base(SkillIds.RG_STRIPARMOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
