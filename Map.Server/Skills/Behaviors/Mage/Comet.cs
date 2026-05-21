using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>WL_COMET — Warlock Comet. Ground unit; ratio +(-100+2500+700*lv); applies SC_MAGIC_POISON.</summary>
public sealed class Comet : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public Comet() : base(SkillIds.WL_COMET) { }
    public Comet(ISkillUnitService? units = null) : base(SkillIds.WL_COMET) => _units = units;
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 2500 + 700 * skillLevel);
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.MagicPoison, val1: skillLevel, 0, 0, 0, durationMs: 20_000, src);
}
