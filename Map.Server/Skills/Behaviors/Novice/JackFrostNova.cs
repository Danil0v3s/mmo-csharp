using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_JACK_FROST_NOVA — Hyper Novice Jack Frost Nova. Manual port of
/// <c>rathena-fork/src/map/skills/novice/jackfrostnova.cpp</c>.
/// Explosion ratio <c>+(-100 + 400 + 500*lv) + 4*SPL</c>; initial drop
/// is <c>+(-100 + 200*lv) + 2*SPL</c>. We use the explosion formula by
/// default. HN_SELFSTUDY_SOCERY amp + SC_RULEBREAK boost are TODO.
/// </summary>
public sealed class JackFrostNova : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public JackFrostNova() : base(SkillIds.HN_JACK_FROST_NOVA) { }

    public JackFrostNova(ISkillUnitService? units = null) : base(SkillIds.HN_JACK_FROST_NOVA)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 + 500 * skillLevel) + 4 * src.Stats.Spl;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
