using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_GRADUAL_GRAVITY — Mob field skill. POS2 unit placement.</summary>
public sealed class IncreasedGravity : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public IncreasedGravity() : base(SkillIds.NPC_GRADUAL_GRAVITY) { }
    public IncreasedGravity(ISkillUnitService? units = null) : base(SkillIds.NPC_GRADUAL_GRAVITY) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
