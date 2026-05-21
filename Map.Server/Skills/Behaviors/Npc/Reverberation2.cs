using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_REVERBERATION — Cell-placed reverberation. Ammo preserved on group-delete.</summary>
public sealed class Reverberation2 : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public Reverberation2() : base(SkillIds.NPC_REVERBERATION) { }
    public Reverberation2(ISkillUnitService? units = null) : base(SkillIds.NPC_REVERBERATION) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
