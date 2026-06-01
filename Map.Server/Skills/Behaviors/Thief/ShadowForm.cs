using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC__SHADOWFORM — Shadow Form. Manual port of
/// <c>rathena-fork/src/map/skills/thief/shadowform.cpp</c>.
/// Casts SC__SHADOWFORM on the caster: Val1 = skillLevel,
/// Val2 = target entity id (the linked partner), Val3 = 4 + skillLevel
/// (reflection-charge count). PC-to-PC only; mob targets or self-cast
/// emit the fail packet without applying.
/// </summary>
public sealed class ShadowForm : SkillImpl
{
    public ShadowForm() : base(SkillIds.SC_SHADOWFORM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena gate: caster must be PC, target must be PC, the two
        // must differ, and the target can't already have someone
        // linked to it (dstsd->shadowform_id == 0). Without a wired
        // ShadowFormId tracker on PlayerEntity we approximate by just
        // applying when both are PCs and src != target; the engine
        // refusal-on-overlap is folded into the SC start path.
        if (src is not PlayerEntity || target is not PlayerEntity || src.Id == target.Id)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, success: false);
            return;
        }

        var sc = ctx.Sc?.Start(src, StatusType.Shadowform,
            val1: skillLevel, val2: (int)target.Id.Value, val3: 4 + skillLevel, val4: 0,
            durationMs: 30_000 + 30_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, sc != null);
    }
}
