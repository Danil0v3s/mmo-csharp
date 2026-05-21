using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_BODYPAINT — Body Painting. Manual port of
/// <c>rathena-fork/src/map/skills/thief/bodypainting.cpp</c>.
/// Splash dispel of Hiding / Cloaking / Camouflage / Stealth /
/// Newmoon / ShadowForm and applies SC_BODYPAINT + Blind at
/// <c>53 + 2*lv</c>% to all enemies in range. Dispel + splash are TODO.
/// </summary>
public sealed class BodyPainting : SkillImpl
{
    public BodyPainting() : base(SkillIds.SC_BODYPAINT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.End(target, StatusType.Hiding);
        ctx.Sc?.End(target, StatusType.Cloaking);
        ctx.Sc?.End(target, StatusType.Cloakingexceed);
        if (System.Random.Shared.Next(100) < 53 + 2 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
