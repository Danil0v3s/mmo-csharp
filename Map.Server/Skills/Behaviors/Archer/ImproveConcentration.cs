using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_CONCENTRATION — Archer Improve Concentration. Mirrors
/// <c>rathena-fork/src/map/skills/archer/improveconcentration.cpp</c>.
///
/// Apply <see cref="StatusType.Concentrate"/> on the caster
/// (+2*lv AGI/DEX). Duration <c>(30 + 30*lv)</c>s.
/// </summary>
public sealed class ImproveConcentration : SkillImpl
{
    public ImproveConcentration() : base(SkillIds.AC_CONCENTRATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Concentrate, val1: 2 * skillLevel, 0, 0, 0,
            durationMs: (30 + 30 * skillLevel) * 1000, src);
    }
}
