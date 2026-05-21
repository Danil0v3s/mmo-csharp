using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_C_MARKER — Rebellion Crimson Marker. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/crimsonmarker.cpp</c>.
/// Marks the target with SC_C_MARKER (val2 = caster id). MAX_SKILL_CRIMSON_MARKER
/// slot tracking is TODO.
/// </summary>
public sealed class CrimsonMarker : SkillImpl
{
    public CrimsonMarker() : base(SkillIds.RL_C_MARKER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var existing = ctx.Sc?.Get(target, StatusType.CMarker);
        if (existing != null && existing.Val2 != (int)src.Id)
            ctx.Sc?.End(target, StatusType.CMarker);
        ctx.Sc?.Start(target, StatusType.CMarker, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
