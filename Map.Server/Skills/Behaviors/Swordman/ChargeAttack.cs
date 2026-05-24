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
        // Deferred: path_search_long pre-flight gate + skill_check_unit_movepos to
        // teleport src adjacent to target needs path_search_long parity in MovementService.
        ctx.UnitOps?.CheckUnitMovePos(src, target.X, target.Y, 1);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
        // Deferred: skill_blown(src, target, blewcount, dir, BLOWN_NONE) after the
        // hit lands — needs src→target direction resolution.
    }
}
