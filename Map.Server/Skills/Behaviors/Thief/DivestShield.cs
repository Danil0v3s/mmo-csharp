using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_STRIPSHIELD — Divest Shield. Manual port of
/// <c>rathena-fork/src/map/skills/thief/divestshield.cpp</c>.
/// Strips the target's shield via
/// <see cref="ISkillSideEffectService.StripEquip"/>; the broadcast
/// carries the strip-success flag so the client renders the correct
/// animation.
/// </summary>
public sealed class DivestShield : SkillImpl
{
    /// <summary>rAthena <c>EQP_SHIELD</c> (0x20) — equip-mask bit consumed
    /// by <c>skill_strip_equip</c>.</summary>
    private const int EQP_SHIELD = 0x20;

    public DivestShield() : base(SkillIds.RG_STRIPSHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var stripped = ctx.SideEffect?.StripEquip(src, target, EQP_SHIELD, durationMs: 0) ?? false;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, stripped);
    }
}
