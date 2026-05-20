using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_THUNDERSTORM — Mage Thunderstorm. Mirrors
/// <c>rathena-fork/src/map/skills/mage/thunderstorm.cpp</c>.
///
/// 3-hit Wind magic AoE on target cell (3×3 splash). Each hit deals
/// (80 + 20*lv)% MATK to every victim in the splash.
/// </summary>
public sealed class Thunderstorm : SkillImpl
{
    private const short SplashRadius = 1;
    private const int HitCount = 3;

    public Thunderstorm() : base(SkillIds.MG_THUNDERSTORM) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = MagicBoltHelper.PerHitDamage(src);
        var rate = 80 + 20 * skillLevel;
        var perHit = Math.Max(1, matk * rate / 100);

        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
            SplashRadius, EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id).ToList();
        for (var hit = 0; hit < HitCount; hit++)
        {
            foreach (var v in victims)
            {
                ctx.Damage.ApplyDamage(v, perHit, src);
            }
        }
    }
}
