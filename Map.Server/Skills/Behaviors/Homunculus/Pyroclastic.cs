using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_PYROCLASTIC — Homunculus Pyroclastic. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_pyroclastic.cpp</c>.
/// Applies SC_PYROCLASTIC to master + target. Master lookup is TODO.
/// </summary>
public sealed class Pyroclastic : SkillImpl
{
    public Pyroclastic() : base(SkillIds.MH_PYROCLASTIC) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Pyroclastic, val1: skillLevel, val2: src.Level, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(src, StatusType.Pyroclastic, val1: skillLevel, val2: src.Level, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
