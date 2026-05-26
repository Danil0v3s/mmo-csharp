using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_STRIPARMOR — Divest Armor. Manual port of
/// <c>rathena-fork/src/map/skills/thief/divestarmor.cpp</c>.
/// Strips the target's armor via
/// <see cref="ISkillSideEffectService.StripEquip"/>; the broadcast
/// frame carries the strip-success flag so the client renders the
/// correct hit / miss animation.
/// </summary>
public sealed class DivestArmor : SkillImpl
{
    /// <summary>rAthena <c>EQP_ARMOR</c> (0x10) — equip-mask bit consumed
    /// by <c>skill_strip_equip</c>.</summary>
    private const int EQP_ARMOR = 0x10;

    public DivestArmor() : base(SkillIds.RG_STRIPARMOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: bool i = skill_strip_equip(...); clif_skill_nodamage(... , i);
        // The strip service owns the SC_STRIPARMOR duration table so
        // the call site doesn't duplicate per-skill numbers.
        var stripped = ctx.SideEffect?.StripEquip(src, target, EQP_ARMOR, durationMs: 0) ?? false;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, stripped);
    }
}
