using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_ICEMINE — Cell-placed ice mine. Ammo preserved on group-delete.</summary>
public sealed class IceMine : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public IceMine() : base(SkillIds.NPC_ICEMINE) { }
    public IceMine(ISkillUnitService? units = null) : base(SkillIds.NPC_ICEMINE) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
