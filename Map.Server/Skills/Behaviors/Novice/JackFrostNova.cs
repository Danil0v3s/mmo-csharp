using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_JACK_FROST_NOVA — Hyper Novice Jack Frost Nova. Port of
/// <c>rathena-fork/src/map/skills/novice/jackfrostnova.cpp</c>.
///
/// Explosion ratio: <c>-100 + 400 + 500·lv + 4·SPL</c>.
/// Initial drop variant: <c>-100 + 200·lv + 2·SPL</c> (we use explosion).
/// Mastery: <c>+ HN_SELFSTUDY_SOCERY · 3 · lv</c> then SOCERY%.
/// SC_RULEBREAK: <c>· (100 + 70) / 100</c>.
/// </summary>
public sealed class JackFrostNova : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public JackFrostNova() : base(SkillIds.HN_JACK_FROST_NOVA) { }

    public JackFrostNova(ISkillUnitService? units = null) : base(SkillIds.HN_JACK_FROST_NOVA)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        int ratio = baseRatio + (-100 + 400 + 500 * skillLevel) + 4 * src.Stats.Spl;
        ratio = HyperNoviceFormulas.ApplySoceryBoost(ratio, src, skillLevel, perLevel: 3, ctx);
        ratio = HyperNoviceFormulas.ApplyRuleBreakBoost(ratio, src, pct: 70, ctx);
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
