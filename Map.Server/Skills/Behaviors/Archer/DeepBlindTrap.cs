using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_DEEPBLINDTRAP — Wind Hawk Deep Blind Trap (4th-class Ranger).
/// Manual port of <c>rathena-fork/src/map/skills/archer/deepblindtrap.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 850*lv + 5*CON)</c> with a <c>20*WH_ADVANCED_TRAP</c>
/// multiplier (TODO — passive read not surfaced). Drops a trap unit.</para>
/// </summary>
public sealed class DeepBlindTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public DeepBlindTrap() : base(SkillIds.WH_DEEPBLINDTRAP) { }

    public DeepBlindTrap(ISkillUnitService? units = null) : base(SkillIds.WH_DEEPBLINDTRAP)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 850 * skillLevel + 5 * src.Stats.Con);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
