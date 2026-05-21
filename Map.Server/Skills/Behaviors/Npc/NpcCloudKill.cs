using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_CLOUD_KILL — Cell-placed poison cloud. Ratio -100+50*lv.</summary>
public sealed class NpcCloudKill : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public NpcCloudKill() : base(SkillIds.NPC_CLOUD_KILL) { }
    public NpcCloudKill(ISkillUnitService? units = null) : base(SkillIds.NPC_CLOUD_KILL) { _units = units; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 50 * skillLevel);
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
