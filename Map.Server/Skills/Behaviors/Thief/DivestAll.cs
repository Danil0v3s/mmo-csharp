using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ST_FULLSTRIP — Divest All. Manual port of
/// <c>rathena-fork/src/map/skills/thief/divestall.cpp</c>.
/// Strips weapon + helm + armor + shield from the target. Strip
/// service + FCP-failure prompt are TODO.
/// </summary>
public sealed class DivestAll : SkillImpl
{
    public DivestAll() : base(SkillIds.ST_FULLSTRIP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
