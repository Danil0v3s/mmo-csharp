using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.HighWizard;

/// <summary>
/// HW_MAGICPOWER — High Wizard Mystical Amplification. Mirrors
/// <c>rathena-fork/src/map/skills/highwizard/magicpower.cpp</c>.
///
/// Apply <see cref="StatusType.Magicpower"/> on caster: next magic
/// cast deals (50 + 5*lv)% extra damage, then SC ends. Duration
/// short (60 s) but normally consumed by the next cast.
/// </summary>
public sealed class MagicPower : SkillImpl
{
    public MagicPower() : base(SkillIds.HW_MAGICPOWER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Magicpower, val1: 50 + 5 * skillLevel, 0, 0, 0,
            durationMs: 60_000, src);
    }
}
