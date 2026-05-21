using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_WINDMILL — Sura Windmill. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/windmill.cpp</c>.
///
/// <para>Centered AoE around the caster. Per-victim post-hit effect:
/// PC targets get a deferred re-hit (skill_addtimerskill); mob
/// targets get SC_STUN for 1-3 seconds at 100 % chance.</para>
///
/// <para>Ratio: <c>-100 + casterBaseLv + casterDex</c> (capped at
/// the caster's level + dex, RE_LVL_DMOD applied at calc time).</para>
/// </summary>
public sealed class Windmill : RecursiveDamageSplashSkillImpl
{
    private readonly Map.Server.Skills.ISkillTimerService? _timers;

    public Windmill() : base(SkillIds.SR_WINDMILL) { }

    public Windmill(Map.Server.Skills.ISkillTimerService? timers = null) : base(SkillIds.SR_WINDMILL)
    {
        _timers = timers;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is PlayerEntity)
        {
            // rAthena: skill_addtimerskill(src, tick+amotion, target->id, 0, 0, getSkillId(), skill_lv, BF_WEAPON, 0);
            // Deferred re-hit on PC targets — re-runs the weapon-attack pipeline.
            _timers?.Schedule(src, target, delayMs: src.Stats.Amotion, SkillId, skillLevel,
                (s, t, lv) =>
                {
                    // Apply a second hit on the deferred tick.
                    CastendDamageId(s, t, lv, ctx);
                });
        }
        else if (target is MobEntity)
        {
            // rAthena: sc_start(SC_STUN, 100%, lv, 1000 + 1000 * (rnd%3))
            var duration = 1000 + 1000 * Random.Shared.Next(3);
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel,
                0, 0, 0, duration, src);
        }
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + status_get_lv(src) + sstatus->dex;
        return baseRatio + (-100 + src.Level + src.Stats.Dex);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: clif_skill_nodamage + skill_castend_damage_id on src (self-centered AoE).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        CastendDamageId(src, src, skillLevel, ctx);
    }
}
