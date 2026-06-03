using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_HUUMARANKA — Swirling Petal. rAthena <c>battle_calc_attack_skill_ratio</c>
/// KO_HUUMARANKA arm (battle.cpp:5647):
/// <c>skillratio += -100 + 150*skill_lv + sstatus->str + (sd ? pc_checkskill(sd,NJ_HUUMA)*100 : 0)</c>,
/// <c>RE_LVL_DMOD(100)</c>, then the SC_KAGEMUSYA caster multiply
/// (<c>skillratio += skillratio * val2/100</c>).
///
/// <para>COMBAT-91 — this recursive-splash arm now routes each victim through
/// <see cref="SkillImpl.ComputeSkillDamage"/> (swing × ratio, with RE_LVL_DMOD and the
/// KAGEMUSYA close), so the partner-skill (<c>NJ_HUUMA*100</c>) term and COMBAT-75's
/// <c>CalculateSkillRatioPostDmodMultiply</c> KAGEMUSYA bonus actually apply.</para>
/// </summary>
public sealed class SwirlingPetal : RecursiveDamageSplashSkillImpl
{
    public SwirlingPetal() : base(SkillIds.KO_HUUMARANKA) { }

    // RE_LVL_DMOD(100) — the SkillImpl default; stated for parity clarity.
    protected override int ReLvlDivisor => 100;

    // battle.cpp:5648 — base ratio (the SC_KAGEMUSYA multiply is the post-dmod close below).
    // pc_checkskill(NJ_HUUMA) is PC-only; a non-PC caster contributes 0 (rAthena `: 0`).
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var huuma = src is PlayerEntity sd ? sd.LearnedSkills.GetValueOrDefault(SkillIds.NJ_HUUMA) : (byte)0;
        return baseRatio + (-100 + 150 * skillLevel) + src.Stats.Str + huuma * 100;
    }

    // COMBAT-75 — SC_KAGEMUSYA caster multiply, applied AFTER RE_LVL_DMOD on the full ratio
    // (battle.cpp:5650). Mirror KoCrossSlash.
    protected override int CalculateSkillRatioPostDmodMultiply(int ratio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext? ctx)
        => ApplyKagemusyaRatio(ratio, src, ctx);

    // COMBAT-91 — per-victim damage: a full skill-aware weapon swing × the per-skill ratio,
    // through the shared ComputeSkillDamage pipeline (ratio → RE_LVL_DMOD → KAGEMUSYA).
    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, victim, SkillId);
        return ComputeSkillDamage(swing, src, victim, skillLevel, ctx);
    }
}
