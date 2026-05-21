using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_LIGHT_OF_REGENE — Homunculus Light of Regene. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_lightofregene.cpp</c>.
/// Applies SC_LIGHT_OF_REGENE to master + self. Master lookup TODO.
/// </summary>
public sealed class LightOfRegene : SkillImpl
{
    public LightOfRegene() : base(SkillIds.MH_LIGHT_OF_REGENE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.LightOfRegene, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(src, StatusType.LightOfRegene, val1: skillLevel, val2: src.Level, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
