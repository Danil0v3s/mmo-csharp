using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_RIDEINLIGHTNING — Sura Ride In Lightning. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/rideinlightening.cpp</c>.
///
/// <para>Ground-target Wind splash. Hits scale with skill level
/// (PC casters get <c>max(1, lv)</c> hits). Ratio:
/// <c>-100 + 40 * lv</c>; +<c>50 * lv</c> bonus when wielding a
/// Knuckle (weapon-type gate deferred — equip introspection
/// not surfaced).</para>
/// </summary>
public sealed class RideInLightening : RecursiveDamageSplashSkillImpl
{
    public RideInLightening() : base(SkillIds.SR_RIDEINLIGHTNING) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        dmg.Hits = src is PlayerEntity ? Math.Max(1, (int)skillLevel) : 1;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 40 * skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Splash damage at the target cell — runs through the per-victim
        // weapon-attack hook on every entity in range.
        const short splashRange = 7;
        var victims = ctx.Entities.ForEachInRange(src.MapId, x, y, splashRange,
            EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id);

        foreach (var v in victims)
        {
            CastendDamageId(src, v, skillLevel, ctx);
        }
    }
}
