using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_FIREWALK — Self elemental walk (fire property weapon).</summary>
public sealed class NpcFireWalk : SkillImpl
{
    public NpcFireWalk() : base(SkillIds.NPC_FIREWALK) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        ctx.Sc?.Start(src, StatusType.Propertywalk, val1: skillLevel, val2: (int)Map.Server.Status.BattleElement.Fire, 0, 0, durationMs: 60_000, src);
    }
}
