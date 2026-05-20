using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Whitesmith;

/// <summary>
/// WS_MELTDOWN — Whitesmith Melt Down. Mirrors
/// <c>rathena-fork/src/map/skills/whitesmith/meltdown.cpp</c>.
///
/// Apply <see cref="StatusType.Meltdown"/> on caster — chance to
/// break enemy weapons/armor on hit. Duration <c>60 + 30*lv</c>s.
/// </summary>
public sealed class MeltDown : SkillImpl
{
    public MeltDown() : base(SkillIds.WS_MELTDOWN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Meltdown, val1: skillLevel, 0, 0, 0,
            durationMs: (60 + 30 * skillLevel) * 1000, src);
    }
}
