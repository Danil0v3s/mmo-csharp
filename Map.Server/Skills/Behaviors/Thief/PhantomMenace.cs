using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_PHANTOMMENACE — Phantom Menace. Manual port of
/// <c>rathena-fork/src/map/skills/thief/phantommenace.cpp</c>.
/// +200 ratio; splash that ONLY hits stealthed targets (Hiding /
/// Cloaking / Cloaking Exceed / Camouflage / Stealthfield).
/// Cloaking Exceed ends on hit; Shadowform breaks on the
/// <c>100 - val1*10</c>% roll without dealing damage.
/// </summary>
public sealed class PhantomMenace : WeaponSkillImpl
{
    /// <summary>rAthena <c>skill_get_splash(GC_PHANTOMMENACE, lv)</c> —
    /// 5×5 splash (radius 2).</summary>
    private const short SPLASH_RADIUS = 2;

    public PhantomMenace() : base(SkillIds.GC_PHANTOMMENACE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena: map_foreachinrange(skill_area_sub, src, splash,
        //   BL_CHAR, ..., flag | BCT_ENEMY | 1, skill_castend_damage_id)
        // → per-victim recurse routes through castendDamageId. We
        // enumerate directly and gate on the stealth predicate.
        foreach (var v in ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y,
                     SPLASH_RADIUS, EntityType.Mob | EntityType.Pc))
        {
            if (v.Id == src.Id) continue;
            HitIfStealthed(src, v, skillLevel, ctx);
        }
    }

    private void HitIfStealthed(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var isStealthed = ctx.Sc.Get(victim, StatusType.Hiding) != null
            || ctx.Sc.Get(victim, StatusType.Cloaking) != null
            || ctx.Sc.Get(victim, StatusType.Cloakingexceed) != null
            || ctx.Sc.Get(victim, StatusType.Camouflage) != null
            || ctx.Sc.Get(victim, StatusType.Stealthfield) != null;
        if (isStealthed)
        {
            ctx.Sc.End(victim, StatusType.Cloakingexceed);
            base.CastendDamageId(src, victim, skillLevel, ctx);
        }
        // Shadowform break — no damage dealt, only SC end.
        var shadow = ctx.Sc.Get(victim, StatusType.Shadowform);
        if (shadow != null && System.Random.Shared.Next(100) < 100 - shadow.Val1 * 10)
            ctx.Sc.End(victim, StatusType.Shadowform);
    }
}
