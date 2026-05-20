using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// KN_BOWLINGBASH (id 62) — Knight Bowling Bash. rAthena
/// <c>skill.cpp:case KN_BOWLINGBASH</c>: physical hit on primary
/// target at (100 + 40 * lv)% ATK, plus splash hits on every enemy
/// within radius 2 of the primary target. Each splash victim takes
/// the same damage; total hit count tops out at the renewal cap
/// (each victim hit once per cast).
///
/// rAthena models knockback on each victim — we record the damage
/// hits today; knockback rides on the movement-direction port.
/// </summary>
public sealed class BowlingBashBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.KN_BOWLINGBASH;

    private const short SplashRadius = 2;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 100 + 40 * skillLevel;

        // Primary hit.
        ApplyHit(source, target, rate, ctx);

        // Splash around primary target — every enemy within radius 2
        // takes the same hit. Skip the source + the primary target.
        var victims = ctx.Entities.ForEachInRange(
            target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var victim in victims)
        {
            if (victim.Id == source.Id) continue;
            if (victim.Id == target.Id) continue;
            ApplyHit(source, victim, rate, ctx);
        }
        return true;
    }

    private static void ApplyHit(Entity src, Entity tgt, int rate, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, tgt);
        var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
        ctx.Damage.ApplyDamage(tgt, dmg, src);
    }
}
