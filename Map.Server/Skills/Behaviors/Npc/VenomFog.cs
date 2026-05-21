using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_VENOMFOG — Cell-placed poison fog. Splash unit placement.</summary>
public sealed class VenomFog : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public VenomFog() : base(SkillIds.NPC_VENOMFOG) { }
    public VenomFog(ISkillUnitService? units = null) : base(SkillIds.NPC_VENOMFOG) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
