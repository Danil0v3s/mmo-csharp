using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_ASTRAL_STRIKE — Arch Mage Astral Strike. Manual port of
/// <c>rathena-fork/src/map/skills/mage/astralstrike.cpp</c>.
///
/// <para>Ground unit + AOE Ghost magic. Ratio:
/// <c>+(-100 + 300 + 1800*lv) + 10*SPL</c>, with +<c>100 + 300*lv</c>
/// against Undead and Dragon races.</para>
/// </summary>
public sealed class AstralStrike : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly ISkillAttackService? _skillAttack;

    public AstralStrike() : base(SkillIds.AG_ASTRAL_STRIKE) { }

    public AstralStrike(ISkillUnitService? units = null, ISkillAttackService? skillAttack = null) : base(SkillIds.AG_ASTRAL_STRIKE)
    {
        _units = units;
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 300 + 1800 * skillLevel) + 10 * src.Stats.Spl;
        if (target.Stats.Race == BattleRace.Undead || target.Stats.Race == BattleRace.Dragon)
            ratio += 100 + 300 * skillLevel;
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
