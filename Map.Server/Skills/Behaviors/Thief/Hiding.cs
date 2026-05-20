using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_HIDING — Thief Hiding. Mirrors
/// <c>rathena-fork/src/map/skills/thief/hiding.cpp</c>.
///
/// Toggle <see cref="StatusType.Hiding"/> on the caster. Recast
/// while hidden ends the SC.
/// </summary>
public sealed class Hiding : StatusSkillImpl
{
    public Hiding() : base(SkillIds.TF_HIDING, endIfRunning: true) { }

    protected override StatusType TargetSc => StatusType.Hiding;

    public override void ApplyAdditionalEffects(Map.Server.Entities.Entity src,
        Map.Server.Entities.Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var durationMs = 60_000 + 30_000 * skillLevel;
        ctx.Sc?.Start(src, StatusType.Hiding, val1: skillLevel, 0, 0, 0, durationMs, src);
    }
}
