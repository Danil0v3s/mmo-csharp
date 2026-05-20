using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_NAPALMBEAT — Mage Napalm Beat. Mirrors
/// <c>rathena-fork/src/map/skills/mage/napalmbeat.cpp</c>.
///
/// Ghost-element AoE on target cell (3×3 splash). Total damage
/// (70 + 10*lv)% MATK is split evenly across all victims in radius.
/// </summary>
public sealed class NapalmBeat : SkillImpl
{
    private const short SplashRadius = 1; // 3x3

    public NapalmBeat() : base(SkillIds.MG_NAPALMBEAT) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
            SplashRadius, EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id).ToList();
        if (victims.Count == 0) return;

        var matk = MagicBoltHelper.PerHitDamage(src);
        var rate = 70 + 10 * skillLevel;
        var totalDamage = Math.Max(1, matk * rate / 100);
        var perVictim = Math.Max(1, totalDamage / victims.Count);
        foreach (var v in victims)
        {
            ctx.Damage.ApplyDamage(v, perVictim, src);
        }
    }
}
