using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// MG_FROSTDIVER (id 15) — Mage Frost Diver. rAthena
/// <c>skill.cpp:case MG_FROSTDIVER</c>: single-target Water magic hit
/// + freeze chance <c>3*lv + 30</c>%. Freeze duration is on the SC's
/// own status_db table (3 s base, MDEF-scaled).
///
/// Damage flows through the Magic resolver (skill_db handles MATK
/// scaling + Water element); plugin layers the Freeze proc.
/// </summary>
public sealed class FrostDiverBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.MG_FROSTDIVER;

    private readonly Random _rng;
    public FrostDiverBehavior(Random? rng = null) { _rng = rng ?? Random.Shared; }

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null)
        {
            var chance = 3 * skillLevel + 30;
            if (_rng.Next(100) < chance)
            {
                // 3 s base; MDEF resistance ports later.
                ctx.Sc.Start(target, StatusType.Freeze, val1: 1, 0, 0, 0,
                    durationMs: 3_000, source);
            }
        }
        return false; // Magic resolver handles damage.
    }
}
