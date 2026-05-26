using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_STRIPWEAPON — Divest Weapon. Manual port of
/// <c>rathena-fork/src/map/skills/thief/divestweapon.cpp</c>.
/// Strips the target's weapon via
/// <see cref="ISkillSideEffectService.StripEquip"/>; the broadcast
/// carries the strip-success flag so the client renders the correct
/// animation.
/// </summary>
public sealed class DivestWeapon : SkillImpl
{
    /// <summary>rAthena <c>EQP_WEAPON</c> (0x02) — right-hand weapon
    /// equip-mask bit consumed by <c>skill_strip_equip</c>.</summary>
    private const int EQP_WEAPON = 0x2;

    public DivestWeapon() : base(SkillIds.RG_STRIPWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var stripped = ctx.SideEffect?.StripEquip(src, target, EQP_WEAPON, durationMs: 0) ?? false;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, stripped);
    }
}
