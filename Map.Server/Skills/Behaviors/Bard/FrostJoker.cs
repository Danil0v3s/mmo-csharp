using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Bard;

/// <summary>
/// BA_FROSTJOKER — Bard Frost Joker. Mirrors
/// <c>rathena-fork/src/map/skills/bard/frostjoker.cpp</c>.
///
/// Caster-centered AoE (radius 7). Each enemy rolls (10 + 5*lv)%
/// chance to be frozen. No damage; pure CC proc.
/// </summary>
public sealed class FrostJoker : SkillImpl
{
    private const short Radius = 7;
    private readonly Random _rng;

    public FrostJoker(Random? rng = null) : base(SkillIds.BA_FROSTJOKER)
    {
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var chance = 10 + 5 * skillLevel;
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, Radius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            if (_rng.Next(100) < chance)
            {
                ctx.Sc.Start(v, StatusType.Freeze, val1: 1, 0, 0, 0,
                    durationMs: 6_000, src);
            }
        }
    }
}
