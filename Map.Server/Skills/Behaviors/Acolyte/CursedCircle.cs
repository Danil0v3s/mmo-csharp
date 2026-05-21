using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_CURSEDCIRCLE — Sura Cursed Circle. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/cursedcircle.cpp</c>.
///
/// <para>Self-centered AoE that locks every enemy in range into
/// <see cref="StatusType.CursedcircleTarget"/> (target side), then
/// applies <see cref="StatusType.CursedcircleAtker"/> to the caster
/// with Val2 = number of locked targets. The caster is rooted for
/// the SC duration; targets cannot move or attack.</para>
///
/// <para>One Spirit Sphere is consumed per locked target. Boss-
/// class mobs (<c>CLASS_BOSS</c>) are immune.</para>
/// </summary>
public sealed class CursedCircle : SkillImpl
{
    private readonly IPlayerOrbService? _orbs;

    public CursedCircle() : base(SkillIds.SR_CURSEDCIRCLE) { }

    public CursedCircle(IPlayerOrbService? orbs = null) : base(SkillIds.SR_CURSEDCIRCLE)
    {
        _orbs = orbs;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        const short splashRange = 3;
        var maxLocks = src is PlayerEntity sd ? _orbs?.Get(sd, OrbKind.Spirit) ?? 15 : 15;

        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, splashRange,
            EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id)
            .Take(maxLocks)
            .ToList();

        int lockedCount = 0;
        foreach (var v in victims)
        {
            // rAthena: skip boss-class mobs (CLASS_BOSS).
            if (v is MobEntity m && (m.Stats.Mode & MobMode.Mvp) != 0) continue;

            // Apply target-side SC (Val2 = caster id for unlock-on-caster-death).
            var sc = ctx.Sc?.Start(v, StatusType.CursedcircleTarget,
                val1: skillLevel, val2: src.Id.Value,
                0, 0, durationMs: 4000, src);
            if (sc != null) lockedCount++;
        }

        // Consume one Spirit Sphere per locked target.
        if (src is PlayerEntity sd2 && lockedCount > 0)
        {
            _orbs?.Remove(sd2, OrbKind.Spirit, lockedCount);
        }

        // Apply caster-side SC (Val2 = count of locked targets).
        ctx.Sc?.Start(src, StatusType.CursedcircleAtker,
            val1: skillLevel, val2: lockedCount,
            0, 0, durationMs: 4000, src);

        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
