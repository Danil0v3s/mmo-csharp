using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SG_STAR_WARM — Warmth of the Stars. POS2 unit placement.</summary>
public sealed class WarmthoftheStars : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public WarmthoftheStars() : base(SkillIds.SG_STAR_WARM) { }
    public WarmthoftheStars(ISkillUnitService? units = null) : base(SkillIds.SG_STAR_WARM) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
