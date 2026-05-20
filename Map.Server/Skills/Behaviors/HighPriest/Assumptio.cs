using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.HighPriest;

/// <summary>
/// HP_ASSUMPTIO — High Priest Assumptio. Mirrors
/// <c>rathena-fork/src/map/skills/highpriest/assumptio.cpp</c>.
///
/// Apply <see cref="StatusType.Assumptio"/> on target — physical
/// damage taken halved. Val1 = lv (controls DEF/MDEF multiplier
/// when the existing Assumptio SC handler reads it). Duration
/// <c>20 + 20*lv</c> seconds.
/// </summary>
public sealed class Assumptio : SkillImpl
{
    public Assumptio() : base(SkillIds.HP_ASSUMPTIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Assumptio, val1: skillLevel, 0, 0, 0,
            durationMs: (20 + 20 * skillLevel) * 1000, src);
    }
}
