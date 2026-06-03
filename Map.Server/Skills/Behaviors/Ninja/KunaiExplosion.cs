using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_BAKURETSU — Kunai Explosion. rAthena <c>battle_calc_attack_skill_ratio</c>
/// KO_BAKURETSU arm (battle.cpp:5663):
/// <c>skillratio += -100 + (sd ? pc_checkskill(sd,NJ_TOBIDOUGU) : 1)*(50 + sstatus->dex/4)*skill_lv*4/10</c>,
/// <c>RE_LVL_DMOD(120)</c>, then <c>skillratio += 10*(sd ? job_level : 1)</c> (post-dmod),
/// then the SC_KAGEMUSYA caster multiply (<c>skillratio += skillratio * val2/100</c>).
///
/// <para>COMBAT-91 — routes each splash victim through <see cref="SkillImpl.ComputeSkillDamage"/>
/// so the real <c>pc_checkskill(NJ_TOBIDOUGU)</c> factor, the post-dmod <c>+10*job_level</c>
/// (NOT scaled by RE_LVL_DMOD(120)), and COMBAT-75's KAGEMUSYA close all apply.</para>
/// </summary>
public sealed class KunaiExplosion : RecursiveDamageSplashSkillImpl
{
    public KunaiExplosion() : base(SkillIds.KO_BAKURETSU) { }

    // COMBAT-91 — RE_LVL_DMOD(120) (battle.cpp:5666).
    protected override int ReLvlDivisor => 120;

    // battle.cpp:5664 — base ratio. pc_checkskill(NJ_TOBIDOUGU) is the real learned level for a
    // PC (0 if unlearned → no contribution); a non-PC caster uses the rAthena `: 1` fallback.
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var tobidougu = src is PlayerEntity sd ? sd.LearnedSkills.GetValueOrDefault(SkillIds.NJ_TOBIDOUGU) : (byte)1;
        return baseRatio + -100 + tobidougu * (50 + src.Stats.Dex / 4) * skillLevel * 4 / 10;
    }

    // battle.cpp:5667 — the `+10*job_level` is added AFTER RE_LVL_DMOD(120), so it must NOT be
    // scaled by the macro: a PC uses its job level, a non-PC the rAthena `: 1` fallback (→ +10).
    protected override int CalculateSkillRatioPostDmod(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext? ctx)
        => 10 * (src is PlayerEntity sd ? sd.JobLevel : 1);

    // COMBAT-75 — SC_KAGEMUSYA caster multiply, applied AFTER the post-dmod add on the full ratio
    // (battle.cpp:5668). Mirror KoCrossSlash.
    protected override int CalculateSkillRatioPostDmodMultiply(int ratio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext? ctx)
        => ApplyKagemusyaRatio(ratio, src, ctx);

    // COMBAT-91 — per-victim damage: a full skill-aware weapon swing × the per-skill ratio,
    // through the shared ComputeSkillDamage pipeline (ratio → RE_LVL_DMOD(120) → +10*jobLv → KAGEMUSYA).
    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, victim, SkillId);
        return ComputeSkillDamage(swing, src, victim, skillLevel, ctx);
    }
}
