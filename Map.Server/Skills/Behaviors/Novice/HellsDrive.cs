using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_HELLS_DRIVE — Hyper Novice Hell's Drive. Port of
/// <c>rathena-fork/src/map/skills/novice/hellsdrive.cpp</c>.
///
/// Base ratio: <c>-100 + 1700 + 900·lv + 3·SPL</c>.
/// Mastery (post-base): <c>+ pc_checkskill(HN_SELFSTUDY_SOCERY) · 4 · lv</c>.
/// Mastery (post-RE_LVL_DMOD): <c>skillratio · SOCERY% / 100</c>.
/// SC_RULEBREAK: <c>skillratio · 70 / 100</c> (after mastery amplifier).
///
/// Caster-side splash dispatched from <see cref="CastendNoDamageId"/>;
/// per-target hit lands in <see cref="CastendDamageId"/>.
/// </summary>
public sealed class HellsDrive : SkillImpl
{
    public HellsDrive() : base(SkillIds.HN_HELLS_DRIVE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        int ratio = baseRatio + (-100 + 1700 + 900 * skillLevel) + 3 * src.Stats.Spl;
        ratio = HyperNoviceFormulas.ApplySoceryBoost(ratio, src, skillLevel, perLevel: 4, ctx);
        ratio = HyperNoviceFormulas.ApplyRuleBreakBoost(ratio, src, pct: 70, ctx);
        return ratio;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
