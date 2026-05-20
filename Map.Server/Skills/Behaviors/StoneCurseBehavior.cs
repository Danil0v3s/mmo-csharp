using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MG_STONECURSE (id 16) — Mage Stone Curse. rAthena
/// <c>skill.cpp:case MG_STONECURSE</c>: single-target Earth magic hit
/// + petrify chance <c>24 + 2*lv</c>%. The petrify itself goes through
/// SC_STONEWAIT (5 s warmup that locks animation) then SC_STONE (the
/// damage / no-action state).
///
/// We attach SC_STONEWAIT here; the Stone handler progresses to
/// SC_STONE on its own once the warmup expires (status engine port
/// of the multi-stage transition lands later — for now both SCs
/// exist as presence markers).
/// </summary>
public sealed class StoneCurseBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MG_STONECURSE;

    private readonly Random _rng;
    public StoneCurseBehavior(Random? rng = null) { _rng = rng ?? Random.Shared; }

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null)
        {
            var chance = 24 + 2 * skillLevel;
            if (_rng.Next(100) < chance)
            {
                // 5 s petrify warmup.
                ctx.Sc.Start(target, StatusType.Stonewait, val1: 1, 0, 0, 0,
                    durationMs: 5_000, source);
            }
        }
        return false; // Magic resolver handles damage.
    }
}
