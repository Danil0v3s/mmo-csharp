using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_B_TRAP — Rebellion Bind Trap. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/bindtrap.cpp</c>.
/// CastendPos2 drops the trap; CastendDamageId hits the bound target.
/// </summary>
public sealed class BindTrap : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    private readonly ISkillUnitService? _units;

    public BindTrap() : base(SkillIds.RL_B_TRAP) { }

    public BindTrap(ISkillAttackService? skillAttack = null, ISkillUnitService? units = null) : base(SkillIds.RL_B_TRAP)
    {
        _skillAttack = skillAttack;
        _units = units;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
