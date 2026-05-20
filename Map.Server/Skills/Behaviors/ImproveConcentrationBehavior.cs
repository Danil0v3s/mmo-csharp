using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// AC_CONCENTRATION (id 45) — Archer Improve Concentration. rAthena
/// <c>skill.cpp:case AC_CONCENTRATION</c>: applies
/// <see cref="StatusType.Concentrate"/> on the caster (+AGI/+DEX).
/// Renewal: +lv*2% AGI + DEX, plus reveals hidden targets in radius
/// (the reveal pulse rides on the cast-radius scan).
///
/// Duration <c>(30 + 30 * lv) * 1000</c> ms.
/// </summary>
public sealed class ImproveConcentrationBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.AC_CONCENTRATION;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // Approximation: +2*lv flat (Concentrate's Val1 reads as agi/dex bonus).
        var agiBonus = 2 * skillLevel;
        var durationMs = (30 + 30 * skillLevel) * 1000;
        ctx.Sc.Start(source, StatusType.Concentrate, val1: agiBonus, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
