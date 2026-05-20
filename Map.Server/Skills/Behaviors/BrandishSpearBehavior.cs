using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// KN_BRANDISHSPEAR (id 57) — Knight Brandish Spear. rAthena
/// <c>skill.cpp:case KN_BRANDISHSPEAR</c>: cone-shaped splash from
/// the caster toward the target — primary hit at (110 + 20*lv)% ATK,
/// secondary cells in the cone take 50 % each.
///
/// First-port approximation: primary target full damage + 5×5 splash
/// around the target at half damage. True cone-shape lands when the
/// directional-cell geometry helper ports (rAthena uses
/// <c>skill_attack_area_dir</c> for the cone walk).
/// </summary>
public sealed class BrandishSpearBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.KN_BRANDISHSPEAR;

    private const short SplashRadius = 2;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 110 + 20 * skillLevel;
        // Primary hit.
        var swing = ctx.Battle.CalcWeaponAttack(source, target);
        var primaryDmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
        ctx.Damage.ApplyDamage(target, primaryDmg, source);

        // Cone splash (approximated as 5×5 around target).
        var splashDmg = Math.Max(1, primaryDmg / 2);
        var victims = ctx.Entities.ForEachInRange(
            target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == source.Id || v.Id == target.Id) continue;
            ctx.Damage.ApplyDamage(v, splashDmg, source);
        }
        return true;
    }
}
