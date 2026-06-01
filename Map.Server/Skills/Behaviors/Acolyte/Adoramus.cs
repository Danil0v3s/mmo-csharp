using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_ADORAMUS — Arch Bishop Adoramus. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/adoramus.cpp</c>.
///
/// <para>Holy magic with splash. Inherits the recursive-splash base
/// (fork: <c>SkillImplRecursiveDamageSplash</c>). Lands a Blind-like
/// SC (<see cref="StatusType.Adoramus"/>) on hit; chance scales with
/// skill level + caster job level.</para>
///
/// <para>Damage ratio: <c>+ (-100 + 300 + 250*lv) = (200 + 250*lv) %</c>
/// (lv1 = 450 %, lv10 = 2700 %) with <c>RE_LVL_DMOD(100)</c>
/// renewal level modifier applied at calc time.</para>
/// </summary>
public sealed class Adoramus : RecursiveDamageSplashSkillImpl
{
    public Adoramus() : base(SkillIds.AB_ADORAMUS) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start(src,target, SC_ADORAMUS, skill_lv*4 + (sd ? job_lv : 50)/2, skill_lv, skill_get_time2(...));
        var jobLv = src is PlayerEntity pcCaster ? pcCaster.JobLevel : 50;
        var chance = skillLevel * 4 + jobLv / 2;

        // Duration: skill_db.yml Duration2 for AB_ADORAMUS — 6000 + 4000*lv ms.
        var duration = (6 + 4 * skillLevel) * 1000;
        // SKILL-01: pass the raw rate (chance% → *100); the SC engine rolls
        // (Adoramus isn't a stat-resisted CC, so it lands at the rolled rate,
        // matching rAthena's default sc_start path for non-resisted SCs).
        ctx.Sc?.Start(target, StatusType.Adoramus, rate: chance * 100,
            val1: skillLevel, 0, 0, 0, duration, source: src);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 300 + 250 * skill_lv; RE_LVL_DMOD(100);
        // RE_LVL_DMOD is the renewal-level-divisor — the C# BattleCalculator
        // applies its equivalent during damage calc (caster's BaseLv / 100).
        return baseRatio + (-100 + 300 + 250 * skillLevel);
    }
}
