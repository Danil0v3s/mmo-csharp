using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// SM_MAGNUM — Swordsman Magnum Break (skill id 7). Mirrors the
/// rAthena case at <c>skill.cpp:skill_castend_damage_id:case SM_MAGNUM</c>:
///
/// <list type="bullet">
///   <item>360° splash within radius 2 around the caster — every enemy
///         in the 5×5 area takes the hit, not just the targeted one.</item>
///   <item>Damage scales as 120% + 20% per level
///         (lv1 = 120%, lv5 = 220%, lv10 = 320%).</item>
///   <item>Applies <c>SC_FIREWEAPON</c> on the caster for ~10 s,
///         endowing autoattacks with Fire (renewal).</item>
///   <item>Costs 2% MaxHp on top of the SP cost (handled by the cost
///         pipeline elsewhere; we just trigger the SC + splash here).</item>
/// </list>
/// </summary>
public sealed class MagnumBreakBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.SM_MAGNUM;

    /// <summary>Splash radius in cells (rAthena: <c>skill_get_splash(SM_MAGNUM,lv) = 2</c>).</summary>
    private const short SplashRadius = 2;

    /// <summary>SC_FIREWEAPON duration in ms (rAthena: 10 s flat).</summary>
    private const int FireWeaponDurationMs = 10_000;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Damage rate: 120 + 20*lv  → lv1 = 140 (well, rAthena uses
        // 120 + 20*lv strictly so lv1 = 140%; we follow that).
        var rate = 120 + 20 * skillLevel;

        // 5×5 splash centered on the caster. Enumerate mobs + players;
        // friendly-fire is gated downstream by DamageService.CanDamage.
        var victims = ctx.Entities.ForEachInRange(
            source.MapId, source.X, source.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);

        var hits = 0;
        foreach (var victim in victims)
        {
            if (victim.Id == source.Id) continue;            // self
            if (!IsAlive(victim)) continue;
            var swing = ctx.Battle.CalcWeaponAttack(source, victim);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(victim, dmg, source);
            hits++;
        }

        // Caster gains SC_FIREWEAPON for 10s (Fire-element autoattacks).
        ctx.Sc?.Start(source, StatusType.Fireweapon, val1: skillLevel, 0, 0, 0,
            durationMs: FireWeaponDurationMs);

        // Even if no one was in radius, we successfully handled the cast —
        // the SC still fired and the cast time / SP / cooldown completed.
        return true;
    }

    private static bool IsAlive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => false,
    };
}
