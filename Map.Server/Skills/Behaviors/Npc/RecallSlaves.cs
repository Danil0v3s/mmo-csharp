using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_RUN_OR_SUMMON_SLAVES — Recall mob slaves to master position.</summary>
public sealed class RecallSlaves : SkillImpl
{
    public RecallSlaves() : base(SkillIds.NPC_CALLSLAVE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
