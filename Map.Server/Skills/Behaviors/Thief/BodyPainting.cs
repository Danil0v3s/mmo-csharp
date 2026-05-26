using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC__BODYPAINT — Body Painting. Manual port of
/// <c>rathena-fork/src/map/skills/thief/bodypainting.cpp</c>.
/// Splash that ends every active stealth SC on each enemy in range
/// (Hiding / Cloaking / Cloaking Exceed / Camouflage / Newmoon, plus
/// Shadowform with a <c>100 - 10*lv</c>% break chance) and applies
/// SC__BODYPAINT + Blind (<c>53 + 2*lv</c>%) to every target — the
/// Blind roll runs on non-hidden targets too per the rAthena comment.
/// </summary>
public sealed class BodyPainting : SkillImpl
{
    /// <summary>rAthena <c>skill_get_splash(SC_BODYPAINT, lv)</c> — 5×5
    /// splash centered on the cast target.</summary>
    private const short SPLASH_RADIUS = 2;

    public BodyPainting() : base(SkillIds.SC_BODYPAINT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena: map_foreachinallrange(skill_area_sub, target, splash,
        // BL_CHAR, … skill_castend_nodamage_id, flag|1) — the |1 branch
        // is what runs the per-victim dispel + apply chain.
        foreach (var v in ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y,
                     SPLASH_RADIUS, EntityType.Mob | EntityType.Pc))
        {
            if (v.Id == src.Id) continue;
            ApplyToVictim(src, v, skillLevel, ctx);
        }
    }

    private static void ApplyToVictim(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null)
        {
            // Stealth-class SCs that flip off when paint touches them.
            ctx.Sc.End(victim, StatusType.Hiding);
            ctx.Sc.End(victim, StatusType.Cloaking);
            ctx.Sc.End(victim, StatusType.Cloakingexceed);
            ctx.Sc.End(victim, StatusType.Camouflage);
            ctx.Sc.End(victim, StatusType.Newmoon);

            // Shadowform breaks at [100 - val1*10]%.
            var shadow = ctx.Sc.Get(victim, StatusType.Shadowform);
            if (shadow != null && System.Random.Shared.Next(100) < 100 - shadow.Val1 * 10)
                ctx.Sc.End(victim, StatusType.Shadowform);

            // ASPD slow always lands; skill_get_time on rAthena scales
            // ~10s base + 2s/lv.
            ctx.Sc.Start(victim, StatusType.Bodypaint, val1: skillLevel, 0, 0, 0,
                durationMs: 10_000 + 2_000 * skillLevel, src);
        }

        if (ctx.Sc != null && System.Random.Shared.Next(100) < 53 + 2 * skillLevel)
            ctx.Sc.Start(victim, StatusType.Blind, val1: skillLevel, 0, 0, 0,
                durationMs: 4_000 + 1_000 * skillLevel, src);
    }
}
