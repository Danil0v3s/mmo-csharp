using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_FIREBALL — Mage Fireball. Mirrors
/// <c>rathena-fork/src/map/skills/mage/fireball.cpp</c>.
///
/// Fire magic single + splash: primary full damage, splash victims
/// half. Damage = (50 + 70*lv)% MATK on primary.
/// </summary>
public sealed class Fireball : SkillImpl
{
    private const short SplashRadius = 2;

    public Fireball() : base(SkillIds.MG_FIREBALL) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = MagicBoltHelper.PerHitDamage(src);
        var rate = 50 + 70 * skillLevel;
        var primaryDmg = Math.Max(1, matk * rate / 100);
        ctx.Damage.ApplyDamage(target, primaryDmg, src);

        var splashDmg = Math.Max(1, primaryDmg / 2);
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
            SplashRadius, EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id || v.Id == target.Id) continue;
            ctx.Damage.ApplyDamage(v, splashDmg, src);
        }
    }
}
