using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_CANE_OF_EVIL_EYE — Cell unit placement at target.</summary>
public sealed class CaneOfEvilEye : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public CaneOfEvilEye() : base(SkillIds.NPC_CANE_OF_EVIL_EYE) { }
    public CaneOfEvilEye(ISkillUnitService? units = null) : base(SkillIds.NPC_CANE_OF_EVIL_EYE) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
