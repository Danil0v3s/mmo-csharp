using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SOA_TOTEM_OF_TUTELARY — POS2 unit placement (totem cell).</summary>
public sealed class TotemOfTutelary : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public TotemOfTutelary() : base(SkillIds.SOA_TOTEM_OF_TUTELARY) { }
    public TotemOfTutelary(ISkillUnitService? units = null) : base(SkillIds.SOA_TOTEM_OF_TUTELARY) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
