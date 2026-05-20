using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Paladin;

/// <summary>
/// PA_SACRIFICE — Paladin Sacrifice (Devotion). Mirrors
/// <c>rathena-fork/src/map/skills/paladin/sacrifice.cpp</c>.
///
/// Apply <see cref="StatusType.Sacrifice"/> on target — redirects
/// physical damage from target to caster (devotion link). Caster
/// dies if HP can't absorb the redirected hit. Duration <c>300 +
/// 60*lv</c> seconds.
/// </summary>
public sealed class Sacrifice : SkillImpl
{
    public Sacrifice() : base(SkillIds.PA_SACRIFICE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Sacrifice, val1: skillLevel, 0, 0, 0,
            durationMs: (300 + 60 * skillLevel) * 1000, src);
    }
}
