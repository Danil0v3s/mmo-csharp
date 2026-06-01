using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_VITALITYACTIVATION — Rune Knight Vitality Activation. Manual
/// port of <c>rathena-fork/src/map/skills/swordman/vitalityactivation.cpp</c>.
///
/// <para>Requires RK_RUNEMASTERY ≥ 2 (gated via
/// <see cref="IPlayerSkillService.CheckSkill"/>). Applies
/// SC_VITALITYACTIVATION.</para>
/// </summary>
public sealed class VitalityActivation : SkillImpl
{
    public VitalityActivation() : base(SkillIds.RK_VITALITYACTIVATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        if ((ctx.PlayerSkill?.CheckSkill(pc, SkillIds.RK_RUNEMASTERY) ?? 0) < 2) return;
        ctx.Sc?.Start(target, StatusType.Vitalityactivation, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
