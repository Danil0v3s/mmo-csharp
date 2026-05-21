using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_VITALITYACTIVATION — Rune Knight Vitality Activation. Manual
/// port of <c>rathena-fork/src/map/skills/swordman/vitalityactivation.cpp</c>.
/// Requires RK_RUNEMASTERY ≥ 2 (TODO). Applies SC_VITALITYACTIVATION.
/// </summary>
public sealed class VitalityActivation : SkillImpl
{
    public VitalityActivation() : base(SkillIds.RK_VITALITYACTIVATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: gate on pc_checkskill(sd, RK_RUNEMASTERY) >= 2.
        ctx.Sc?.Start(target, StatusType.Vitalityactivation, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
