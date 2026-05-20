using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Dancer;

/// <summary>
/// DC_SCREAM — Dancer Scream. Mirrors
/// <c>rathena-fork/src/map/skills/dancer/scream.cpp</c>.
///
/// Caster-centered AoE (radius 7). Each enemy rolls (5 + 5*lv)%
/// chance to be stunned for 5 s. No damage.
/// </summary>
public sealed class Scream : SkillImpl
{
    private const short Radius = 7;
    private const int StunMs = 5_000;
    private readonly Random _rng;

    public Scream(Random? rng = null) : base(SkillIds.DC_SCREAM)
    {
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var chance = 5 + 5 * skillLevel;
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, Radius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            if (_rng.Next(100) < chance)
            {
                ctx.Sc.Start(v, StatusType.Stun, val1: 1, 0, 0, 0, StunMs, src);
            }
        }
    }
}
