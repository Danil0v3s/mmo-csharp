using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// DC_SCREAM (id 326) — Dancer Scream. rAthena
/// <c>skill.cpp:case DC_SCREAM</c>: caster-centered AoE, every enemy
/// in radius 7 rolls (5 + 5*lv)% chance to be stunned. Mirrors
/// <see cref="FrostJokerBehavior"/> but for Stun instead of Freeze.
/// </summary>
public sealed class ScreamBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.DC_SCREAM;

    private const short Radius = 7;
    private const int StunDurationMs = 5_000;

    private readonly Random _rng;
    public ScreamBehavior(Random? rng = null) { _rng = rng ?? Random.Shared; }

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        var chance = 5 + 5 * skillLevel;
        var victims = ctx.Entities.ForEachInRange(
            source.MapId, source.X, source.Y, Radius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == source.Id) continue;
            if (_rng.Next(100) < chance)
            {
                ctx.Sc.Start(v, StatusType.Stun, val1: 1, 0, 0, 0,
                    StunDurationMs, source);
            }
        }
        return true;
    }
}
