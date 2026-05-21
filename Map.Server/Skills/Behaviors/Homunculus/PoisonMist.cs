using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_POISON_MIST — Homunculus Poison Mist. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_poisonmist.cpp</c>.
/// Drops the poison cloud unit. Ratio <c>+(-100 + 200*lv*BaseLv/100) + DEX</c>.
/// </summary>
public sealed class PoisonMist : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public PoisonMist() : base(SkillIds.MH_POISON_MIST) { }

    public PoisonMist(ISkillUnitService? units = null) : base(SkillIds.MH_POISON_MIST)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel * src.Level / 100) + src.Stats.Dex;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
