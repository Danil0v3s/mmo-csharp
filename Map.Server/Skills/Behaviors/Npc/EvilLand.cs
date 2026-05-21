using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_EVILLAND — POS2 unit placement.</summary>
public sealed class EvilLand : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public EvilLand() : base(SkillIds.NPC_EVILLAND) { }
    public EvilLand(ISkillUnitService? units = null) : base(SkillIds.NPC_EVILLAND) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
