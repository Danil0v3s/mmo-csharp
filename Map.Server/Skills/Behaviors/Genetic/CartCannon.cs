using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Genetic;

/// <summary>
/// GN_CARTCANNON — Genetic Cart Cannon. Mirrors
/// <c>rathena-fork/src/map/skills/genetic/cartcannon.cpp</c>.
///
/// Single-target ranged at (60 + 60*lv * (1 + int/40))% damage with
/// element scaled by loaded cannonball. Requires cart + cannonball
/// (cost upstream).
/// </summary>
public sealed class CartCannon : SkillImpl
{
    public CartCannon() : base(SkillIds.GN_CARTCANNON) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, target);
        var rate = (60 + 60 * skillLevel) * (40 + src.Stats.IntStat) / 40;
        var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
        ctx.Damage.ApplyDamage(target, Math.Max(1, dmg), src);
    }
}
