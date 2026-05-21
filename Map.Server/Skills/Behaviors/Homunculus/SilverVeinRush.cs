using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_SILVERVEIN_RUSH — Homunculus Silvervein Rush. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_silverveinrush.cpp</c>.
/// Ratio <c>+(-100 + 250*lv*BaseLv/100) + STR</c>.
/// </summary>
public sealed class SilverVeinRush : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public SilverVeinRush() : base(SkillIds.MH_SILVERVEIN_RUSH) { }

    public SilverVeinRush(ISkillAttackService? skillAttack = null) : base(SkillIds.MH_SILVERVEIN_RUSH)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 250 * skillLevel * src.Level / 100) + src.Stats.Str;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
