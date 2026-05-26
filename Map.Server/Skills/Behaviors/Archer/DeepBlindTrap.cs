using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_DEEPBLINDTRAP — Wind Hawk Deep Blind Trap (4th-class Ranger).
/// Manual port of <c>rathena-fork/src/map/skills/archer/deepblindtrap.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 850*lv + 5*CON)</c>; the running ratio is
/// then scaled by <c>ratio * (20 * WH_ADVANCED_TRAP) / 100</c>. rAthena
/// uses the max passive level (5) when the caster isn't a player.
/// Drops a trap ground unit on the targeted cell.</para>
/// </summary>
public sealed class DeepBlindTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public DeepBlindTrap() : base(SkillIds.WH_DEEPBLINDTRAP) { }

    public DeepBlindTrap(ISkillUnitService? units = null) : base(SkillIds.WH_DEEPBLINDTRAP)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 850 * skillLevel + 5 * src.Stats.Con);
        var adv = (src is PlayerEntity pc)
            ? (ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WH_ADVANCED_TRAP) ?? 0)
            : 5;
        ratio += ratio * (20 * adv) / 100;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
