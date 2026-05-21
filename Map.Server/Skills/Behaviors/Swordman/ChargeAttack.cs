using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_CHARGEATK — Knight Charge Attack. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/chargeattack.cpp</c>.
/// Renewal: fixed ratio +600. Teleports the caster next to the target
/// (if path clear) and lands a single hit with knockback equal to
/// skill_get_blewcount. Teleport + knockback wiring is TODO; we just
/// run the weapon strike.
/// </summary>
public sealed class ChargeAttack : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public ChargeAttack() : base(SkillIds.KN_CHARGEATK) { }

    public ChargeAttack(ISkillAttackService? skillAttack = null) : base(SkillIds.KN_CHARGEATK)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 600;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: path_search_long + skill_check_unit_movepos to teleport src next to target.
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
        // TODO: skill_blown(src, target, blewcount, dir, BLOWN_NONE) after a successful hit.
    }
}
