using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_C_MARKER — Rebellion Crimson Marker. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/crimsonmarker.cpp</c>.
/// Marks the target with SC_C_MARKER (val2 = caster id). The caster's
/// marker list is reverse-resolved by walking sc state on entities;
/// <see cref="ClearForCaster"/> wraps the rAthena
/// <c>pc_crimson_marker_clear</c> (pc.cpp:14925) — used on map_quit /
/// caster death / weapon unequip.
/// </summary>
public sealed class CrimsonMarker : SkillImpl
{
    /// <summary>rAthena <c>MAX_SKILL_CRIMSON_MARKER</c> — caps active markers per caster.</summary>
    public const int MaxMarkersPerCaster = 3;

    public CrimsonMarker() : base(SkillIds.RL_C_MARKER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var existing = ctx.Sc?.Get(target, StatusType.CMarker);
        if (existing != null && existing.Val2 != (int)src.Id)
            ctx.Sc?.End(target, StatusType.CMarker);
        ctx.Sc?.Start(target, StatusType.CMarker, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    /// <summary>
    /// rAthena <c>pc_crimson_marker_clear</c> (pc.cpp:14925). Walks the
    /// entity registry looking for SC_C_MARKER instances whose Val2
    /// (caster id) matches <paramref name="caster"/>; ends each.
    /// Called from pc_quit / death / weapon-unequip flows.
    /// </summary>
    public static int ClearForCaster(Entity caster, IEntityRegistry entities, IStatusChangeService? sc)
    {
        if (sc == null) return 0;
        var cleared = 0;
        foreach (var e in entities.All())
        {
            var marker = sc.Get(e, StatusType.CMarker);
            if (marker == null) continue;
            if (marker.Val2 != caster.Id.Value) continue;
            sc.End(e, StatusType.CMarker);
            cleared++;
        }
        return cleared;
    }
}
