using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Knight;

/// <summary>
/// KN_BRANDISHSPEAR — Knight Brandish Spear. Mirrors
/// <c>rathena-fork/src/map/skills/swordman/brandishspear.cpp</c>.
///
/// Primary hit at (110 + 20*lv)% ATK; splash victims around the target
/// take half. True cone-shape (rAthena uses skill_attack_area_dir)
/// ports when the directional-cell helper lands.
/// </summary>
public sealed class BrandishSpear : RecursiveDamageSplashSkillImpl
{
    public BrandishSpear() : base(SkillIds.KN_BRANDISHSPEAR) { }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 2;

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, victim);
        var rate = 110 + 20 * skillLevel;
        return swing.Total * rate / 100 / 2; // half-damage splash; primary gets the full hit below.
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Primary hit (full damage).
        var swing = ctx.Battle.CalcWeaponAttack(src, target);
        var rate = 110 + 20 * skillLevel;
        var primaryDmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
        ctx.Damage.ApplyDamage(target, primaryDmg, src);
        // Splash victims (half damage) — base traversal skips the primary
        // via id check + the source.
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
            GetSplashSearchSize(src, skillLevel), GetSplashTarget(src));
        foreach (var v in victims)
        {
            if (v.Id == src.Id || v.Id == target.Id) continue;
            var dmg = SplashDamage(src, v, skillLevel, ctx);
            if (dmg > 0) ctx.Damage.ApplyDamage(v, (int)Math.Clamp(dmg, 0, int.MaxValue), src);
        }
    }
}
