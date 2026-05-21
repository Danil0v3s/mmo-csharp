using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_RAY_OF_PROTECTION — All Ray of Protection. Manual port of
/// <c>rathena-fork/src/map/skills/other/rayofprotection.cpp</c>.
/// Buff SC; enum not yet in StatusType — TODO. Animation lands.
/// </summary>
public sealed class RayOfProtection : SkillImpl
{
    public RayOfProtection() : base(SkillIds.ALL_RAY_OF_PROTECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
