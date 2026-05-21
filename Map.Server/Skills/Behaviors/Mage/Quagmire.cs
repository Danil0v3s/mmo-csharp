using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>WZ_QUAGMIRE — Wizard Quagmire. Ground unit placement (AGI/DEX-debuff zone).</summary>
public sealed class Quagmire : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public Quagmire() : base(SkillIds.WZ_QUAGMIRE) { }
    public Quagmire(ISkillUnitService? units = null) : base(SkillIds.WZ_QUAGMIRE) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
