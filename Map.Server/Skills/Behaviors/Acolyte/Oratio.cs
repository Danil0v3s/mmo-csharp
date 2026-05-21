using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_ORATIO — Arch Bishop Oratio. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/oratio.cpp</c>.
///
/// <para>AoE Holy-resistance debuff centered on the target. Splash
/// iteration applies <see cref="StatusType.Oratio"/> at
/// <c>(40 + 5 * skillLevel) %</c> chance per enemy in range.</para>
///
/// <para>Duration: <c>30000 ms</c> per <c>db/re/skill_db.yml</c>.</para>
/// </summary>
public sealed class Oratio : SkillImpl
{
    private readonly Random _rng;

    public Oratio() : base(SkillIds.AB_ORATIO) => _rng = Random.Shared;

    public Oratio(Random? rng = null) : base(SkillIds.AB_ORATIO) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: outer pass broadcasts; inner per-victim pass applies the SC.
        const short splashRange = 7;
        var chance = 40 + 5 * skillLevel;
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y,
            splashRange, EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id);

        foreach (var v in victims)
        {
            if (_rng.Next(100) >= chance) continue;
            ctx.Sc?.Start(v, StatusType.Oratio,
                val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        }

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
