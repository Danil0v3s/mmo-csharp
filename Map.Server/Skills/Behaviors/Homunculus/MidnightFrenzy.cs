using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_MIDNIGHT_FRENZY — Homunculus Midnight Frenzy. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_midnightfrenzy.cpp</c>.
/// Ratio <c>+(-100 + 450*lv*BaseLv/150) + STR</c>.
/// </summary>
public sealed class MidnightFrenzy : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public MidnightFrenzy() : base(SkillIds.MH_MIDNIGHT_FRENZY) { }

    public MidnightFrenzy(ISkillAttackService? skillAttack = null) : base(SkillIds.MH_MIDNIGHT_FRENZY)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 450 * skillLevel * src.Level / 150) + src.Stats.Str;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
