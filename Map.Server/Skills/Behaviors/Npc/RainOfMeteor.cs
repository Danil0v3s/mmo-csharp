using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_RAINOFMETEOR — Cell-placed meteor rain. Splash unit placement TODO.</summary>
public sealed class RainOfMeteor : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public RainOfMeteor() : base(SkillIds.NPC_RAINOFMETEOR) { }
    public RainOfMeteor(ISkillUnitService? units = null) : base(SkillIds.NPC_RAINOFMETEOR) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
