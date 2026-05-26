using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_STRIPHELM — Divest Helm. Manual port of
/// <c>rathena-fork/src/map/skills/thief/divesthelm.cpp</c>.
/// Strips the target's helm via
/// <see cref="ISkillSideEffectService.StripEquip"/>; the broadcast
/// carries the strip-success flag so the client renders the correct
/// animation.
/// </summary>
public sealed class DivestHelm : SkillImpl
{
    /// <summary>rAthena <c>EQP_HEAD_TOP</c> (0x100) — top-head equip-mask
    /// bit consumed by <c>skill_strip_equip</c>.</summary>
    private const int EQP_HEAD_TOP = 0x100;

    public DivestHelm() : base(SkillIds.RG_STRIPHELM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var stripped = ctx.SideEffect?.StripEquip(src, target, EQP_HEAD_TOP, durationMs: 0) ?? false;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, stripped);
    }
}
