using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_POISON — Thief Envenom (Poison). Mirrors
/// <c>rathena-fork/src/map/skills/thief/poison.cpp</c>.
///
/// Apply <see cref="StatusType.Poison"/> with success rate (30 + 5*lv)%.
/// Duration <c>25 * lv</c>s. Poison DoT runs through the SC handler
/// (1.5 %/sec MaxHp).
/// </summary>
public sealed class Poison : SkillImpl
{
    private readonly Random _rng;

    public Poison(Random? rng = null) : base(SkillIds.TF_POISON)
    {
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var chance = 30 + 5 * skillLevel;
        if (_rng.Next(100) < chance)
        {
            ctx.Sc.Start(target, StatusType.Poison, val1: 1, 0, 0, 0,
                durationMs: 25_000 * skillLevel, src);
        }
    }
}
