using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SG_SUN_WARM — Warmth of the Sun. POS2 unit placement (warm-aura cell).</summary>
public sealed class WarmthoftheSun : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public WarmthoftheSun() : base(SkillIds.SG_SUN_WARM) { }
    public WarmthoftheSun(ISkillUnitService? units = null) : base(SkillIds.SG_SUN_WARM) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
