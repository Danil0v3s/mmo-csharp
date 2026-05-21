using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_LAVA_SLIDE — Homunculus Lava Slide. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_lavaslide.cpp</c>.
/// Ratio <c>+(-100 + 50*lv)</c>. Drops the lava unit at (x, y).
/// </summary>
public sealed class LavaSlide : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public LavaSlide() : base(SkillIds.MH_LAVA_SLIDE) { }

    public LavaSlide(ISkillUnitService? units = null) : base(SkillIds.MH_LAVA_SLIDE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 50 * skillLevel);

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
