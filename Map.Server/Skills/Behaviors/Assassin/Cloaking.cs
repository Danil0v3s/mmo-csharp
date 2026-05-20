using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Assassin;

/// <summary>
/// AS_CLOAKING — Assassin Cloaking. Mirrors
/// <c>rathena-fork/src/map/skills/assassin/cloaking.cpp</c>.
///
/// Toggle <see cref="StatusType.Cloaking"/> on the caster. Slower
/// SP drain than Hiding and can walk while invisible at higher
/// levels.
/// </summary>
public sealed class Cloaking : StatusSkillImpl
{
    public Cloaking() : base(SkillIds.AS_CLOAKING, endIfRunning: true) { }

    protected override StatusType TargetSc => StatusType.Cloaking;

    public override void ApplyAdditionalEffects(Map.Server.Entities.Entity src,
        Map.Server.Entities.Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var durationMs = 120_000 + 60_000 * (skillLevel - 1);
        ctx.Sc?.Start(src, StatusType.Cloaking, val1: skillLevel, 0, 0, 0, durationMs, src);
    }
}
