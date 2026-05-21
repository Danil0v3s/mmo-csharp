using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_THORNS_TRAP — Genetic Thorn Trap. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/thorntrap.cpp</c>.
/// CastendPos2 drops the unit; CastendDamageId hits the trapped target
/// via the skill's regular attack type.
/// </summary>
public sealed class ThornTrap : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    private readonly ISkillUnitService? _units;

    public ThornTrap() : base(SkillIds.GN_THORNS_TRAP) { }

    public ThornTrap(ISkillAttackService? skillAttack = null, ISkillUnitService? units = null) : base(SkillIds.GN_THORNS_TRAP)
    {
        _skillAttack = skillAttack;
        _units = units;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
