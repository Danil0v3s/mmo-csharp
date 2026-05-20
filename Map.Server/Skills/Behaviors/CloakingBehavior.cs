using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// AS_CLOAKING (id 135) — Assassin Cloaking. rAthena
/// <c>skill.cpp:case AS_CLOAKING</c>: toggles
/// <see cref="StatusType.Cloaking"/> on the caster. Slower SP-drain
/// than Hiding (TF_HIDING) and can move while invisible at higher
/// levels. Re-cast while cloaked ends the SC.
///
/// Duration <c>120_000 + 60_000 * (lv-1)</c> ms — capped by SP cost
/// upstream when the SP-drain interval ticks down to zero.
/// </summary>
public sealed class CloakingBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.AS_CLOAKING;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // Toggle: re-cast ends.
        if (ctx.Sc.Get(source, StatusType.Cloaking) != null)
        {
            ctx.Sc.End(source, StatusType.Cloaking);
            return true;
        }

        var durationMs = 120_000 + 60_000 * (skillLevel - 1);
        ctx.Sc.Start(source, StatusType.Cloaking, val1: skillLevel, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
