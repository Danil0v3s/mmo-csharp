using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_EARTHSPIKE — Wizard Earth Spike. Single-target Earth magic.
/// Renewal ratio: <c>+100</c>; SC_EARTH_CARE_OPTION 9× multiplier deferred.
/// </summary>
public sealed class EarthSpike : SkillImpl
{
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;
    public EarthSpike() : base(SkillIds.WZ_EARTHSPIKE) { }
    public EarthSpike(Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.WZ_EARTHSPIKE) => _skillAttack = skillAttack;
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100;
}
