using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_WEAPONCRUSH — Weapon Crush. Manual port of
/// <c>rathena-fork/src/map/skills/thief/weaponcrush.cpp</c>.
/// Strips the target's weapon via
/// <see cref="ISkillSideEffectService.StripEquip"/>; the broadcast
/// carries the strip-success flag for the client animation.
/// </summary>
public sealed class WeaponCrush : WeaponSkillImpl
{
    /// <summary>rAthena <c>EQP_WEAPON</c> (0x02) — right-hand weapon
    /// equip-mask bit consumed by <c>skill_strip_equip</c>.</summary>
    private const int EQP_WEAPON = 0x2;

    public WeaponCrush() : base(SkillIds.GC_WEAPONCRUSH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena weaponcrush.cpp:castendNoDamageId — bool i = strip;
        // clif_skill_nodamage(... , i). Match the broadcast-on-success
        // semantic; the second castend pass (applyAdditionalEffects)
        // is folded into this single resolve.
        var stripped = ctx.SideEffect?.StripEquip(src, target, EQP_WEAPON, durationMs: 0) ?? false;
        if (stripped)
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, true);
    }
}
