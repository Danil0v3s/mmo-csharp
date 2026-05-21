using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_FLAMECROSS — POS2 unit placement (cross fire).</summary>
public sealed class FlameCross : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public FlameCross() : base(SkillIds.NPC_FLAMECROSS) { }
    public FlameCross(ISkillUnitService? units = null) : base(SkillIds.NPC_FLAMECROSS) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
