using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_RAIGEKISAI — Lightning Strike Of Destruction. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/lightningstrikeofdestruction.cpp</c>.
/// Renewal: +100*lv ratio (charm bonus TODO). POS2 unit placement.
/// </summary>
public sealed class LightningStrikeOfDestruction : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public LightningStrikeOfDestruction() : base(SkillIds.NJ_RAIGEKISAI) { }

    public LightningStrikeOfDestruction(ISkillUnitService? units = null) : base(SkillIds.NJ_RAIGEKISAI)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * skillLevel;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
