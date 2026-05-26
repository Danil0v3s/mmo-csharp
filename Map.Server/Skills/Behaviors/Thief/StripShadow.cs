using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_STRIP_SHADOW — Strip Shadow. Manual port of
/// <c>rathena-fork/src/map/skills/thief/stripshadow.cpp</c>.
/// Strips the full shadow-gear set via
/// <see cref="ISkillSideEffectService.StripEquip"/>; the broadcast
/// carries the strip-success flag so the client renders the correct
/// animation.
/// </summary>
public sealed class StripShadow : SkillImpl
{
    /// <summary>rAthena <c>EQP_SHADOW_GEAR</c> — union of every shadow
    /// slot bit (armor / weapon / shield / shoes / both accessories).</summary>
    private const int EQP_SHADOW_GEAR = 0x3F00000;

    public StripShadow() : base(SkillIds.ABC_STRIP_SHADOW) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var stripped = ctx.SideEffect?.StripEquip(src, target, EQP_SHADOW_GEAR, durationMs: 0) ?? false;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, stripped);
    }
}
