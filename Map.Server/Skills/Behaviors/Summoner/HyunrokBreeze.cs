using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_HYUN_ROKS_BREEZE — Shaman Hyun Rok's Breeze. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/hyunrokbreeze.cpp</c>.
/// Ratio <c>+(-100 + 650 + 750*lv) + 5*SPL</c>. Mystical Creature
/// Mastery + Hyun Rok Communion bonuses are TODO.
/// </summary>
public sealed class HyunrokBreeze : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public HyunrokBreeze() : base(SkillIds.SH_HYUN_ROKS_BREEZE) { }

    public HyunrokBreeze(ISkillUnitService? units = null) : base(SkillIds.SH_HYUN_ROKS_BREEZE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 650 + 750 * skillLevel) + 5 * src.Stats.Spl;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
