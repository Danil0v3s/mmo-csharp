using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_TWISTER_CUTTER — Homunculus Twister Cutter. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_twistercutter.cpp</c>.
/// Ratio <c>+(-100 + 480*lv*BaseLv/100) + INT</c>.
/// </summary>
public sealed class TwisterCutter : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public TwisterCutter() : base(SkillIds.MH_TWISTER_CUTTER) { }

    public TwisterCutter(ISkillAttackService? skillAttack = null) : base(SkillIds.MH_TWISTER_CUTTER)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 480 * skillLevel * src.Level / 100) + src.Stats.IntStat;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
