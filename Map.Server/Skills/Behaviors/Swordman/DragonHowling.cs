using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_DRAGONHOWLING — Rune Knight Dragon Howling. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/dragonhowling.cpp</c>.
///
/// <para>Inflicts SC_FEAR at <c>50 + 6*lv</c> % on every enemy in a
/// 5-cell splash around the caster. The splash itself rides on
/// <see cref="IEntityRegistry.ForEachInRange"/> with
/// <c>EntityType.Mob | EntityType.Pc</c> matching rAthena's
/// <c>map_foreachinallrange(BCT_ENEMY | SD_PREAMBLE | 1)</c>.</para>
/// </summary>
public sealed class DragonHowling : SkillImpl
{
    private readonly Random _rng;

    public DragonHowling() : base(SkillIds.RK_DRAGONHOWLING) => _rng = Random.Shared;

    public DragonHowling(Random? rng = null) : base(SkillIds.RK_DRAGONHOWLING)
        => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var rate = 50 + 6 * skillLevel;
        // rAthena: 5-cell splash around the CASTER, BCT_ENEMY filtered.
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, 5, EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            if (_rng.Next(100) < rate)
                ctx.Sc?.Start(v, StatusType.Fear, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        }
    }
}
