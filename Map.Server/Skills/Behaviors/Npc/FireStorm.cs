using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_FIRESTORM — POS2 unit placement (fire field).</summary>
public sealed class FireStorm : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public FireStorm() : base(SkillIds.NPC_FIRESTORM) { }
    public FireStorm(ISkillUnitService? units = null) : base(SkillIds.NPC_FIRESTORM) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
