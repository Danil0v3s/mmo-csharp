using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// BA_FROSTJOKER (id 318) — Bard Frost Joker. rAthena
/// <c>skill.cpp:case BA_FROSTJOKER</c>: caster-centered AoE,
/// every enemy in radius 7 rolls (10 + 5*lv)% chance to be frozen.
/// No damage; pure crowd-control proc — opposite-sex caster only
/// (the joke gate isn't enforced today; rAthena requires the
/// caster + target to be different genders, but that's a roleplay
/// rule the C# port doesn't track yet).
/// </summary>
public sealed class FrostJokerBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.BA_FROSTJOKER;

    private const short Radius = 7;

    private readonly Random _rng;
    public FrostJokerBehavior(Random? rng = null) { _rng = rng ?? Random.Shared; }

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        var chance = 10 + 5 * skillLevel;
        var victims = ctx.Entities.ForEachInRange(
            source.MapId, source.X, source.Y, Radius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == source.Id) continue;
            if (_rng.Next(100) < chance)
            {
                ctx.Sc.Start(v, StatusType.Freeze, val1: 1, 0, 0, 0,
                    durationMs: 6_000, source);
            }
        }
        return true;
    }
}
