using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_SONIC_CRAW — Homunculus Sonic Craw. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_sonicclaw.cpp</c>.
/// Ratio <c>+(-100 + 60*lv*BaseLv/150)</c>. Hit count = spiritballs (TODO).
/// </summary>
public sealed class SonicClaw : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public SonicClaw() : base(SkillIds.MH_SONIC_CRAW) { }

    public SonicClaw(ISkillAttackService? skillAttack = null) : base(SkillIds.MH_SONIC_CRAW)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 60 * skillLevel * src.Level / 150);

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
}
