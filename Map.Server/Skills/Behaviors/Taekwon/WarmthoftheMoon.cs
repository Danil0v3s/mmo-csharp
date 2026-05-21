using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SG_MOON_WARM — Warmth of the Moon. POS2 unit placement.</summary>
public sealed class WarmthoftheMoon : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public WarmthoftheMoon() : base(SkillIds.SG_MOON_WARM) { }
    public WarmthoftheMoon(ISkillUnitService? units = null) : base(SkillIds.SG_MOON_WARM) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
