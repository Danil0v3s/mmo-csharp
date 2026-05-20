using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Knight;

/// <summary>
/// KN_BOWLINGBASH — Knight Bowling Bash. Mirrors
/// <c>rathena-fork/src/map/skills/swordman/bowlingbash.cpp</c>.
///
/// Primary physical hit at (100 + 40*lv)% ATK plus splash hits on
/// every enemy within radius 2 of the primary target. Each splash
/// victim takes the same damage as the primary.
/// </summary>
public sealed class BowlingBash : SkillImpl
{
    private const short SplashRadius = 2;

    public BowlingBash() : base(SkillIds.KN_BOWLINGBASH) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 100 + 40 * skillLevel;
        ApplyHit(src, target, rate, ctx);

        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
            SplashRadius, EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id || v.Id == target.Id) continue;
            ApplyHit(src, v, rate, ctx);
        }
    }

    private static void ApplyHit(Entity src, Entity tgt, int rate, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, tgt);
        var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
        ctx.Damage.ApplyDamage(tgt, dmg, src);
    }
}
