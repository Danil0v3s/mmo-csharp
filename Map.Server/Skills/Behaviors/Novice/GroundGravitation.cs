using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_GROUND_GRAVITATION — Hyper Novice Ground Gravitation. Port of
/// <c>rathena-fork/src/map/skills/novice/groundgravitation.cpp</c>.
///
/// Field-tick variant: <c>-100 + 800 + 700·lv + 2·SPL</c>.
/// Initial drop variant: <c>-100 + 3000 + 1500·lv + 5·SPL</c> (we use
/// field-tick by default).
///
/// Mastery: <c>+ pc_checkskill(HN_SELFSTUDY_SOCERY) · 2 · lv</c> (field)
/// or <c>· 4 · lv</c> (initial). Post-base amplifier: <c>skillratio ·
/// SOCERY% / 100</c>. SC_RULEBREAK: <c>skillratio · 50 / 100</c>.
/// </summary>
public sealed class GroundGravitation : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public GroundGravitation() : base(SkillIds.HN_GROUND_GRAVITATION) { }

    public GroundGravitation(ISkillUnitService? units = null) : base(SkillIds.HN_GROUND_GRAVITATION)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        int ratio = baseRatio + (-100 + 800 + 700 * skillLevel) + 2 * src.Stats.Spl;
        ratio = HyperNoviceFormulas.ApplySoceryBoost(ratio, src, skillLevel, perLevel: 2, ctx);
        ratio = HyperNoviceFormulas.ApplyRuleBreakBoost(ratio, src, pct: 50, ctx);
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
