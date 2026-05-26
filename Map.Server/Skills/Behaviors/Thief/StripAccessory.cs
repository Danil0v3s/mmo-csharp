using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_STRIPACCESSARY — Strip Accessory. Manual port of
/// <c>rathena-fork/src/map/skills/thief/stripaccessory.cpp</c>.
/// Strips the target's accessory slots via
/// <see cref="ISkillSideEffectService.StripEquip"/>; the broadcast
/// carries the strip-success flag so the client renders the correct
/// animation.
/// </summary>
public sealed class StripAccessory : SkillImpl
{
    /// <summary>rAthena <c>EQP_ACC</c> (0x08 | 0x10) — left + right
    /// accessory equip-mask bits consumed by <c>skill_strip_equip</c>.</summary>
    private const int EQP_ACC = 0x8 | 0x10;

    public StripAccessory() : base(SkillIds.SC_STRIPACCESSARY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var stripped = ctx.SideEffect?.StripEquip(src, target, EQP_ACC, durationMs: 0) ?? false;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, stripped);
    }
}
