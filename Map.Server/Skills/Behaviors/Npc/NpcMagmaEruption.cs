using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_MAGMA_ERUPTION — Cell-placed magma. Splash + lava unit placement TODO.</summary>
public sealed class NpcMagmaEruption : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public NpcMagmaEruption() : base(SkillIds.NPC_MAGMA_ERUPTION) { }
    public NpcMagmaEruption(ISkillUnitService? units = null) : base(SkillIds.NPC_MAGMA_ERUPTION) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
