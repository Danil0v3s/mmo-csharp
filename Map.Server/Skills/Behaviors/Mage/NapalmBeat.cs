using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_NAPALMBEAT — Mage Napalm Beat. Manual port of
/// <c>rathena-fork/src/map/skills/mage/napalmbeat.cpp</c>.
///
/// <para>The fork inherits <c>SkillImplRecursiveDamageSplash</c> —
/// a specialized base that handles the splash-damage pipeline +
/// damage-share across victims. The C# port doesn't yet have the
/// matching subclass on its <see cref="SkillImpl"/> hierarchy, so
/// the splash logic is inlined here (3×3 splash, total
/// <c>(70 + 10*lv) %</c> MATK split across all victims).</para>
///
/// <para><see cref="CalculateSkillRatio"/> mirrors the fork's
/// hook exactly: <c>-30 + 10 * skill_lv</c> ratio bump means the
/// effective skill ratio caps at <c>(100 - 30 + 10*lv) = 70 + 10*lv</c>%
/// — same formula as the inline body, just expressed as a ratio
/// adjustment on top of the base 100 %.</para>
/// </summary>
public sealed class NapalmBeat : SkillImpl
{
    private const short SplashRadius = 1; // 3x3 splash matches MG_NAPALMBEAT skill_db SplashArea: 1

    public NapalmBeat() : base(SkillIds.MG_NAPALMBEAT) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena fork: SkillImplRecursiveDamageSplash::castendDamageId(...)
        // which iterates skill_get_splash() cells, calls skill_attack per
        // victim, and adjusts division across hit count for damage-share.
        // We inline the equivalent: gather victims in radius, divide
        // CalculateSkillRatio-adjusted MATK across them.
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
            SplashRadius, EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id).ToList();
        if (victims.Count == 0) return;

        // Apply CalculateSkillRatio (the per-skill modifier) to a base 100 ratio.
        var ratio = CalculateSkillRatio(100, src, target, skillLevel);
        var matk = MagicBoltHelper.PerHitDamage(src);
        var totalDamage = Math.Max(1, matk * ratio / 100);
        var perVictim = Math.Max(1, totalDamage / victims.Count);

        foreach (var v in victims)
        {
            ctx.Damage.ApplyDamage(v, perVictim, src);
        }
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: base_skillratio += -30 + 10 * skill_lv;
        // lv1 = -20 (effective 80 %), lv10 = +70 (effective 170 %).
        return baseRatio + (-30 + 10 * skillLevel);
    }
}
