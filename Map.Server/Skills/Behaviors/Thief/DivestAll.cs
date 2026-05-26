using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ST_FULLSTRIP — Divest All. Manual port of
/// <c>rathena-fork/src/map/skills/thief/divestall.cpp</c>.
/// Strips weapon + helm + armor + shield in one cast via
/// <see cref="ISkillSideEffectService.StripEquip"/>. When the target
/// is fully Full-Chemical-Protected (CP_WEAPON ∧ CP_HELM ∧ CP_ARMOR ∧
/// CP_SHIELD) the cast aborts silently — the rAthena branch emits a
/// Gospel-style info packet (0x28); we match the early-out without
/// the prompt frame.
/// </summary>
public sealed class DivestAll : SkillImpl
{
    /// <summary>rAthena <c>EQP_WEAPON | EQP_ARMOR | EQP_HEAD_TOP | EQP_SHIELD</c> —
    /// every slot ST_FULLSTRIP targets in a single call.</summary>
    private const int EQP_FULLSTRIP = 0x2 | 0x10 | 0x100 | 0x20;

    public DivestAll() : base(SkillIds.ST_FULLSTRIP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null
            && ctx.Sc.Get(target, StatusType.CpWeapon) != null
            && ctx.Sc.Get(target, StatusType.CpHelm) != null
            && ctx.Sc.Get(target, StatusType.CpArmor) != null
            && ctx.Sc.Get(target, StatusType.CpShield) != null)
        {
            return;
        }

        var stripped = ctx.SideEffect?.StripEquip(src, target, EQP_FULLSTRIP, durationMs: 0) ?? false;
        if (stripped)
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, true);
    }
}
