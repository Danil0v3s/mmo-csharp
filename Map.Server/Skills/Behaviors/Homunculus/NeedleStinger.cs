using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_NEEDLE_STINGER — Homunculus Needle Stinger. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_needlestinger.cpp</c>.
/// Ratio <c>+(-100 + 200 + 500*lv*BaseLv/100) + DEX</c>.
/// </summary>
public sealed class NeedleStinger : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public NeedleStinger() : base(SkillIds.MH_NEEDLE_STINGER) { }

    public NeedleStinger(ISkillAttackService? skillAttack = null) : base(SkillIds.MH_NEEDLE_STINGER)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 + 500 * skillLevel * src.Level / 100) + src.Stats.Dex;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
