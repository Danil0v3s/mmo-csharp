using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_FUUMASHOUAKU — Huuma Shuriken Grasp. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/huumashurikengrasp.cpp</c>.
/// POS2 unit placement; ratio <c>+(-100 + 850 + 350*lv) + 5*pow</c>.
/// SS_FUUMAKOUCHIKU partner bonus is TODO.
/// </summary>
public sealed class HuumaShurikenGrasp : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public HuumaShurikenGrasp() : base(SkillIds.SS_FUUMASHOUAKU) { }

    public HuumaShurikenGrasp(ISkillUnitService? units = null) : base(SkillIds.SS_FUUMASHOUAKU)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 850 + 350 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
