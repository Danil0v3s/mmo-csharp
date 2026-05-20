using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Priest;

/// <summary>
/// PR_LEXDIVINA — Priest Lex Divina. Mirrors
/// <c>rathena-fork/src/map/skills/priest/lexdivina.cpp</c>.
///
/// Apply <see cref="StatusType.Silence"/> on target — silenced for
/// <c>30 * lv</c> seconds. Recast on an already-silenced target
/// cures instead (StatusSkillImpl handles this via
/// <see cref="StatusSkillImpl.EndIfRunning"/>).
/// </summary>
public sealed class LexDivina : StatusSkillImpl
{
    public LexDivina() : base(SkillIds.PR_LEXDIVINA, endIfRunning: true) { }

    protected override StatusType TargetSc => StatusType.Silence;

    public override void ApplyAdditionalEffects(Map.Server.Entities.Entity src,
        Map.Server.Entities.Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Silence, val1: skillLevel, 0, 0, 0,
            durationMs: 30_000 * skillLevel, src);
    }
}
