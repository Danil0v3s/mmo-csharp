using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Knight;

/// <summary>
/// KN_TWOHANDQUICKEN — Knight Two-Hand Quicken. Mirrors
/// <c>rathena-fork/src/map/skills/swordman/twohandquicken.cpp</c>.
///
/// Applies <see cref="StatusType.Twohandquicken"/> on the caster.
/// Val1 = 7 * lv (ASPD boost). Duration <c>30 * lv</c> seconds.
/// </summary>
public sealed class TwoHandQuicken : SkillImpl
{
    public TwoHandQuicken() : base(SkillIds.KN_TWOHANDQUICKEN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        ctx.Sc.Start(src, StatusType.Twohandquicken, val1: 7 * skillLevel, 0, 0, 0,
            durationMs: 30_000 * skillLevel, src);
    }
}
