using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// TF_POISON (id 52) — Thief Poison. rAthena
/// <c>skill.cpp:case TF_POISON</c>: applies <see cref="StatusType.Poison"/>
/// on the target with success rate <c>30 + 5 * lv</c>%. Poison ticks
/// 1.5 % MaxHp per 1.5 s (handled by the registered SC handler).
/// Duration <c>25 * lv</c> seconds.
///
/// Does no immediate damage; the Poison DoT runs entirely through
/// the SC engine.
/// </summary>
public sealed class PoisonBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.TF_POISON;

    private readonly Random _rng;
    public PoisonBehavior(Random? rng = null) { _rng = rng ?? Random.Shared; }

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;
        var chance = 30 + 5 * skillLevel;
        if (_rng.Next(100) < chance)
        {
            ctx.Sc.Start(target, StatusType.Poison, val1: 1, 0, 0, 0,
                durationMs: 25_000 * skillLevel, source);
        }
        return true; // Plugin owns the cast — no fall-through damage.
    }
}
